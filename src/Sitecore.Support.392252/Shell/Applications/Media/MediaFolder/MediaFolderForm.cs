using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.Abstractions;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Resources;
using Sitecore.Resources.Media;
using Sitecore.Shell;
using Sitecore.Shell.Applications.FlashUpload.Advanced;
using Sitecore.Web.UI.HtmlControls;

namespace Sitecore.Support.Shell.Applications.Media.MediaFolder
{
    public class MediaFolderForm : Sitecore.Shell.Applications.Media.MediaFolder.MediaFolderForm
    {
        private static readonly bool _resolveMediaItemUsage;

        static MediaFolderForm()
        {
            var setting = Sitecore.Configuration.Settings.GetSetting("Sitecore.Support.392252.ResolveMediaItemUsage", "false");
            if (!bool.TryParse(setting, out _resolveMediaItemUsage))
            {
                _resolveMediaItemUsage = false;
            }
        }

        public MediaFolderForm(BaseTranslate translate, BaseMediaManager mediaManager) : base(translate, mediaManager)
        {
        }

        protected override void RenderItem([NotNull] HtmlTextWriter output, [NotNull] Item item)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(item, "item");

            int size;
            string metaData = string.Empty;
            string validation = string.Empty;
            string usages = string.Empty;

            string src;

            if (UploadedItems.Include(item))
            {
                UploadedItems.RenewExpiration();
            }

            if (this.ShowChildCountInPreview(item))
            {
                src = Themes.MapTheme("Applications/48x48/folder.png");
                size = 48;

                int count = UserOptions.View.ShowHiddenItems ? item.Children.Count : this.GetVisibleChildCount(item);

                metaData = count + " " + this.Translate.Text(count == 1 ? Texts.ITEM1 : Texts.ITEMS);
            }
            else
            {
                MediaItem mediaItem = item;

                MediaUrlOptions queryString = MediaUrlOptions.GetThumbnailOptions(item);

                size = this.MediaManager.HasMediaContent(mediaItem) ? 72 : 48;

                queryString.Width = size;
                queryString.Height = size;
                queryString.UseDefaultIcon = true;

                src = this.MediaManager.GetMediaUrl(mediaItem, queryString);

                MediaMetaDataFormatter mediaMetaDataFormatter = this.MediaManager.Config.GetMetaDataFormatter(mediaItem.Extension);

                if (mediaMetaDataFormatter != null)
                {
                    MediaMetaDataCollection metaDataCollection = mediaItem.GetMetaData();
                    var encodedMetaDataCollection = new MediaMetaDataCollection();

                    foreach (var key in metaDataCollection.Keys)
                    {
                        encodedMetaDataCollection[key] = HttpUtility.HtmlEncode(metaDataCollection[key]);
                    }

                    if (metaData != null)
                    {
                        metaData = mediaMetaDataFormatter.Format(encodedMetaDataCollection, MediaMetaDataFormatterOutput.HtmlNoKeys);
                    }
                }

                MediaValidatorResult mediaValidatorResults = mediaItem.ValidateMedia();

                var validationFormatter = new MediaValidatorFormatter();

                validation = validationFormatter.Format(mediaValidatorResults, MediaValidatorFormatterOutput.HtmlPopup);

               // Resolve links only if setting allows it
               if (_resolveMediaItemUsage)
                {
                    LinkDatabase linkDatabase = Globals.LinkDatabase;

                    ItemLink[] links = linkDatabase.GetReferrers(item);

                    if (links.Length > 0)
                    {
                        usages = links.Length + " " + this.Translate.Text(links.Length == 1 ? Texts.USAGE : Texts.USAGES);
                    }
                }
            }

            var a = new Tag("a");
            a.Add("id", "I" + item.ID.ToShortID());
            a.Add("href", "#");
            a.Add("onclick", "javascript:scForm.getParentForm().invoke('item:load(id=" + item.ID + ")');return false");

            if (UploadedItems.Include(item))
            {
                a.Add("class", "highlight");
            }

            a.Start(output);

            var image = new ImageBuilder
            {
                Src = src,
                Class = "scMediaIcon",
                Width = size,
                Height = size
            };

            string style = string.Empty;

            if (size < 72)
            {
                size = (72 - size) / 2;
                style = string.Format("padding:{0}px {0}px {0}px {0}px", size);
            }

            if (!string.IsNullOrEmpty(style))
            {
                style = " style=\"" + style + "\"";
            }

            output.Write("<div class=\"scMediaBorder\"" + style + ">");

            output.Write(image.ToString());

            output.Write("</div>");

            output.Write("<div class=\"scMediaTitle\">" + item.GetUIDisplayName() + "</div>");

            if (!string.IsNullOrEmpty(metaData))
            {
                output.Write("<div class=\"scMediaDetails\">" + metaData + "</div>");
            }

            if (!string.IsNullOrEmpty(validation))
            {
                output.Write("<div class=\"scMediaValidation\">" + validation + "</div>");
            }

            if (!string.IsNullOrEmpty(usages))
            {
                output.Write("<div class=\"scMediaUsages\">" + usages + "</div>");
            }

            a.End(output);
        }
    }
}