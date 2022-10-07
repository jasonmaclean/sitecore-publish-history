using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using System;
using System.Linq;

namespace SitecoreFundamentals.PublishHistory.Pipelines
{
    public class SetPublishDates : PublishItemProcessor
    {
        public override void Process(PublishItemContext context)
        {
            if (context == null || context.Result == null)
                return;

            PublishOperation operation = context.Result.Operation;

            if ((operation != PublishOperation.Created && operation != PublishOperation.Updated) ||
                (context.Action != PublishAction.PublishVersion)
                || (context.VersionToPublish == null))
                return;

            try
            {
                var item = context.PublishHelper.GetSourceItem(context.ItemId);

                if (item == null)
                {
                    Log.Error($"{Settings.GetSetting("SitecoreFundamentals.PublishHistory.LogPrefix")} Item not found with ID {context.ItemId} ", this);
                    return;
                }

                if (item.Fields[Constants.Templates.PublishHistory.Fields.LastPublished] == null
                    || item.Fields[Constants.Templates.PublishHistory.Fields.LastPublishedBy] == null)
                    return;

                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
                    using (new EditContext(item, false, false))
                    {
                        var publishedByValue = context.User?.Name;
                        if (string.IsNullOrWhiteSpace(publishedByValue) || publishedByValue.Split('\\').Last() == "Anonymous")
                        {
                            var itemUpdatedBy = item[Constants.Templates.Sections.Statistics.UpdatedBy];
                            var updatedBy = !string.IsNullOrWhiteSpace(itemUpdatedBy) ? itemUpdatedBy : "Unknown";

                            publishedByValue = $"{Settings.GetSetting("SitecoreFundamentals.PublishHistory.FieldPrefix")}{updatedBy}";
                        }

                        item[Constants.Templates.PublishHistory.Fields.LastPublished] = DateUtil.ToIsoDate(DateTime.Now);
                        item[Constants.Templates.PublishHistory.Fields.LastPublishedBy] = publishedByValue;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error($"{Settings.GetSetting("SitecoreFundamentals.PublishHistory.LogPrefix")} {ex}", this);
            }
        }
    }
}