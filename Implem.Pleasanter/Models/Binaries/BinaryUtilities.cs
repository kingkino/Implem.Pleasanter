﻿using Implem.DefinitionAccessor;
using Implem.Libraries.Classes;
using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.DataTypes;
using Implem.Pleasanter.Libraries.Extensions;
using Implem.Pleasanter.Libraries.General;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Models;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Resources;
using Implem.Pleasanter.Libraries.Responses;
using Implem.Pleasanter.Libraries.Security;
using Implem.Pleasanter.Libraries.Server;
using Implem.Pleasanter.Libraries.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Implem.Pleasanter.Models
{
    public static class BinaryUtilities
    {
        /// <summary>
        /// Fixed:
        /// </summary>
        public static bool ExistsSiteImage(
            Context context,
            SiteSettings ss,
            long referenceId,
            Libraries.Images.ImageData.SizeTypes sizeType)
        {
            var invalid = BinaryValidators.OnGetting(
                context: context,
                ss: ss);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return false;
            }
            switch (Parameters.BinaryStorage.Provider)
            {
                case "Local":
                    return new Libraries.Images.ImageData(
                        referenceId, Libraries.Images.ImageData.Types.SiteImage)
                            .Exists(sizeType);
                default:
                    return Rds.ExecuteScalar_int(
                        context: context,
                        statements: Rds.SelectBinaries(
                            column: Rds.BinariesColumn().BinariesCount(),
                            where: Rds.BinariesWhere()
                                .ReferenceId(referenceId)
                                .BinaryType("SiteImage"))) == 1;
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string SiteImagePrefix(
            Context context,
            SiteSettings ss,
            long referenceId,
            Libraries.Images.ImageData.SizeTypes sizeType)
        {
            var invalid = BinaryValidators.OnGetting(
                context: context,
                ss: ss);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return string.Empty;
            }
            return new BinaryModel(referenceId).SiteImagePrefix(
                context: context,
                sizeType: sizeType);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static byte[] SiteImageThumbnail(Context context, SiteModel siteModel)
        {
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: siteModel.SiteId);
            var invalid = BinaryValidators.OnGetting(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return null;
            }
            return new BinaryModel(siteModel.SiteId).SiteImage(
                context: context,
                sizeType: Libraries.Images.ImageData.SizeTypes.Thumbnail,
                column: Rds.BinariesColumn().Thumbnail());
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static byte[] SiteImageIcon(Context context, SiteModel siteModel)
        {
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: siteModel.SiteId);
            var invalid = BinaryValidators.OnGetting(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return null;
            }
            return new BinaryModel(siteModel.SiteId).SiteImage(
                context: context,
                sizeType: Libraries.Images.ImageData.SizeTypes.Icon,
                column: Rds.BinariesColumn().Icon());
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string UpdateSiteImage(Context context, SiteModel siteModel)
        {
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: siteModel.SiteId);
            var file = Forms.File(Libraries.Images.ImageData.Types.SiteImage.ToString());
            var invalid = BinaryValidators.OnUploadingSiteImage(
                context: context,
                ss: siteModel.SiteSettings,
                file: file);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson();
            }
            var error = new BinaryModel(siteModel.SiteId).UpdateSiteImage(
                context: context, file: file);
            if (error.Has())
            {
                return error.MessageJson();
            }
            else
            {
                return new ResponseCollection()
                    .Html(
                        "#SiteImageIconContainer",
                        new HtmlBuilder().SiteImageIcon(
                            context: context,
                            ss: siteModel.SiteSettings,
                            siteId: siteModel.SiteId))
                    .Html(
                        "#SiteImageSettingsEditor",
                        new HtmlBuilder().SiteImageSettingsEditor(
                            context: context,
                            ss: siteModel.SiteSettings))
                    .Message(Messages.FileUpdateCompleted())
                    .ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string DeleteSiteImage(Context context, SiteModel siteModel)
        {
            siteModel.SiteSettings = SiteSettingsUtilities.Get(
                context: context,
                siteModel: siteModel,
                referenceId: siteModel.SiteId);
            var invalid = BinaryValidators.OnDeletingSiteImage(
                context: context,
                ss: siteModel.SiteSettings);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson();
            }
            var error = new BinaryModel(siteModel.SiteId)
                .DeleteSiteImage(context: context);
            if (error.Has())
            {
                return error.MessageJson();
            }
            else
            {
                return new ResponseCollection()
                    .Html(
                        "#SiteImageIconContainer",
                        new HtmlBuilder().SiteImageIcon(
                            context: context,
                            ss: siteModel.SiteSettings,
                            siteId: siteModel.SiteId))
                    .Html(
                        "#SiteImageSettingsEditor",
                        new HtmlBuilder().SiteImageSettingsEditor(
                            context: context,
                            ss: siteModel.SiteSettings))
                    .Message(Messages.FileDeleteCompleted())
                    .ToJson();
            }
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string UploadImage(
            Context context, System.Web.HttpPostedFileBase[] files, long id)
        {
            var controlId = Forms.ControlId();
            var ss = new ItemModel(
                context: context,
                referenceId: id).GetSite(
                    context: context,
                    initSiteSettings: true)
                        .SiteSettings;
            var invalid = BinaryValidators.OnUploadingImage(
                context: context,
                files: files);
            switch (invalid)
            {
                case Error.Types.OverTenantStorageSize:
                    return Messages.ResponseOverTenantStorageSize(
                        Contract.TenantStorageSize(context: context).ToString()).ToJson();
                case Error.Types.None: break;
                default: return invalid.MessageJson();
            }
            var guid = Strings.NewGuid();
            var file = files[0];
            var size = file.ContentLength;
            var bin = file.Byte();
            Rds.ExecuteNonQuery(
                context: context,
                statements: Rds.InsertBinaries(
                    param: Rds.BinariesParam()
                        .TenantId(context.TenantId)
                        .ReferenceId(id)
                        .Guid(guid)
                        .BinaryType("Images")
                        .Title(file.FileName)
                        .Bin(bin, _using: !Parameters.BinaryStorage.IsLocal())
                        .FileName(file.FileName)
                        .Extension(file.Extension())
                        .Size(size)
                        .ContentType(file.ContentType)));
            if (Parameters.BinaryStorage.IsLocal())
            {
                bin.Write(System.IO.Path.Combine(Directories.BinaryStorage(), "Images", guid));
            }
            var hb = new HtmlBuilder();
            return new ResponseCollection()
                .InsertText("#" + Forms.ControlId(), $"![image]({Locations.ShowFile(guid)})")
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string DeleteImage(Context context, string guid)
        {
            var binaryModel = new BinaryModel()
                .Get(
                    context: context,
                    where: Rds.BinariesWhere()
                        .TenantId(context.TenantId)
                        .Guid(guid));
            var ss = new ItemModel(
                context: context,
                referenceId: binaryModel.ReferenceId)
                    .GetSite(
                        context: context,
                        initSiteSettings: true)
                            .SiteSettings;
            var invalid = BinaryValidators.OnDeletingImage(
                context: context,
                ss: ss,
                binaryModel: binaryModel);
            switch (invalid)
            {
                case Error.Types.None: break;
                default: return invalid.MessageJson();
            }
            binaryModel.Delete(context: context);
            return new ResponseCollection()
                .Message(Messages.DeletedImage())
                .Remove($"#ImageLib .item[data-id=\"{guid}\"]")
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string MultiUpload(
            Context context, System.Web.HttpPostedFileBase[] files, long id)
        {
            var controlId = Forms.ControlId();
            var ss = new ItemModel(
                context: context,
                referenceId: id).GetSite(
                    context: context,
                    initSiteSettings: true)
                        .SiteSettings;
            var column = ss.GetColumn(
                context: context,
                columnName: Forms.Data("ColumnName"));
            var attachments = Forms.Data("AttachmentsData").Deserialize<Attachments>();
            var invalid = BinaryValidators.OnUploading(
                context: context,
                column: column,
                attachments: attachments,
                files: files);
            switch (invalid)
            {
                case Error.Types.OverLimitQuantity:
                    return Messages.ResponseOverLimitQuantity(
                        column.LimitQuantity.ToString()).ToJson();
                case Error.Types.OverLimitSize:
                    return Messages.ResponseOverLimitSize(
                        column.LimitSize.ToString()).ToJson();
                case Error.Types.OverTotalLimitSize:
                    return Messages.ResponseOverTotalLimitSize(
                        column.TotalLimitSize.ToString()).ToJson();
                case Error.Types.OverTenantStorageSize:
                    return Messages.ResponseOverTenantStorageSize(
                        Contract.TenantStorageSize(context: context).ToString()).ToJson();
                case Error.Types.None: break;
                default: return invalid.MessageJson();
            }
            files.ForEach(file => attachments.Add(new Attachment()
            {
                Guid = file.WriteToTemp(),
                Name = file.FileName.Split('\\').Last(),
                Size = file.ContentLength,
                Extention = file.Extension(),
                ContentType = file.ContentType,
                Added = true,
                Deleted = false
            }));
            var hb = new HtmlBuilder();
            return new ResponseCollection()
                .ReplaceAll($"#{controlId}Field", new HtmlBuilder()
                    .Field(
                        context: context,
                        ss: ss,
                        column: column,
                        value: attachments.ToJson(),
                        columnPermissionType: column
                            .ColumnPermissionType(context: context)))
                .SetData("#" + controlId)
                .ToJson();
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static System.Web.Mvc.FileContentResult Donwload(Context context, string guid)
        {
            if (!Contract.Attachments(context: context))
            {
                return null;
            }
            return FileContentResults.Download(context: context, guid: guid);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static System.Web.Mvc.FileContentResult DownloadTemp(Context context, string guid)
        {
            if (!Contract.Attachments(context: context))
            {
                return null;
            }
            return FileContentResults.DownloadTemp(guid);
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static string DeleteTemp(Context context)
        {
            if (!Contract.Attachments(context: context))
            {
                return null;
            }
            File.DeleteTemp(Forms.Data("Guid"));
            return "[]";
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static decimal UsedTenantStorageSize(Context context)
        {
            return Rds.ExecuteScalar_decimal(
                context: context,
                statements: Rds.SelectBinaries(
                    column: Rds.BinariesColumn().Size(function: Sqls.Functions.Sum),
                    where: Rds.BinariesWhere().TenantId(context.TenantId)));
        }

        /// <summary>
        /// Fixed:
        /// </summary>
        public static SqlStatement UpdateReferenceId(
            Context context, SiteSettings ss, long referenceId, string values)
        {
            var guids = values?.RegexValues("[0-9a-z]{32}").ToList();
            return guids?.Any() == true
                ? Rds.UpdateBinaries(
                    param: Rds.BinariesParam().ReferenceId(referenceId),
                    where: Rds.BinariesWhere()
                        .TenantId(context.TenantId)
                        .ReferenceId(ss.SiteId)
                        .Guid(guids, multiParamOperator: " or ")
                        .Creator(context.UserId))
                : null;
        }
    }
}
