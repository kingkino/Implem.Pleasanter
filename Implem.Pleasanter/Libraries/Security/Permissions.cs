﻿using Implem.DefinitionAccessor;
using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Server;
using Implem.Pleasanter.Libraries.Settings;
using Implem.Pleasanter.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Implem.Pleasanter.Libraries.Security
{
    public static class Permissions
    {
        public enum Types : long
        {
            NotSet = 0,                         // 00000000000000000000000000000000
            Read = 1,                           // 00000000000000000000000000000001
            Create = 2,                         // 00000000000000000000000000000010
            Update = 4,                         // 00000000000000000000000000000100
            Delete = 8,                         // 00000000000000000000000000001000
            SendMail = 16,                      // 00000000000000000000000000010000
            Export = 32,                        // 00000000000000000000000000100000
            Import = 64,                        // 00000000000000000000000001000000
            ManageSite = 128,                   // 00000000000000000000000010000000
            ManagePermission = 256,             // 00000000000000000000000100000000
            ManageTenant = 1073741824,          // 01000000000000000000000000000000
            ManageService = 2147483648,         // 10000000000000000000000000000000
        }

        public static Types Get(string name)
        {
            switch (name)
            {
                case "NotSet": return Types.NotSet;
                case "Read": return Types.Read;
                case "Create": return Types.Create;
                case "Update": return Types.Update;
                case "Delete": return Types.Delete;
                case "SendMail": return Types.SendMail;
                case "Export": return Types.Export;
                case "Import": return Types.Import;
                case "ManageSite": return Types.ManageSite;
                case "ManagePermission": return Types.ManagePermission;
                case "ManageTenant": return Types.ManageTenant;
                case "ManageService": return Types.ManageService;
                default: return Types.NotSet;
            }
        }

        public static List<Permission> Get(List<string> formData, Types? type = null)
        {
            var data = new List<Permission>();
            formData?.ForEach(line =>
            {
                var part = line.Split(',');
                if (part.Count() == 3)
                {
                    data.Add(new Permission(
                        part[0],
                        part[1].ToInt(),
                        type != null
                            ? (Types)type
                            : (Types)part[2].ToLong()));
                }
            });
            return data;
        }

        public static Types General()
        {
            return (Types)Parameters.Permissions.General;
        }

        public static Types Manager()
        {
            return (Types)Parameters.Permissions.Manager;
        }

        public enum ColumnPermissionTypes
        {
            Deny,
            Read,
            Update
        }

        public static Dictionary<long, Types> Get(Context context, IEnumerable<long> targets)
        {
            return Hash(
                dataRows: Rds.ExecuteTable(
                    context: context,
                    statements: Rds.SelectPermissions(
                        distinct: true,
                        column: Rds.PermissionsColumn()
                            .ReferenceId()
                            .PermissionType(),
                        where: Rds.PermissionsWhere()
                            .ReferenceId_In(targets.Where(o => o != 0))
                            .Or(Rds.PermissionsWhere()
                                .GroupId_In(sub: Rds.SelectGroupMembers(
                                    column: Rds.GroupMembersColumn().GroupId(),
                                    where: Rds.GroupMembersWhere()
                                        .Add(raw: DeptOrUser("GroupMembers"))))
                                .Add(raw: DeptOrUser("Permissions")))))
                                    .AsEnumerable());
        }

        public static SqlWhereCollection SetCanReadWhere(
            Context context,
            SiteSettings ss,
            SqlWhereCollection where,
            bool checkPermission = true)
        {
            if (ss.IsSite(context: context) && ss.ReferenceType == "Sites")
            {
                where.Add(
                    tableName: "Sites",
                    raw: $"[Sites].[ParentId] in ({ss.SiteId})");
            }
            else
            {
                if (ss.ColumnHash.ContainsKey("SiteId"))
                {
                    if (ss.AllowedIntegratedSites != null)
                    {
                        where.Or(new SqlWhereCollection()
                            .Add(
                                tableName: ss.ReferenceType,
                                raw: "[{0}].[SiteId] in ({1})".Params(
                                    ss.ReferenceType, ss.AllowedIntegratedSites.Join()))
                            .CheckRecordPermission(ss, ss.IntegratedSites));
                    }
                    else
                    {
                        where.Add(
                            tableName: ss.ReferenceType,
                            raw: "[{0}].[SiteId] in ({1})".Params(
                                ss.ReferenceType, ss.SiteId));
                        if (!context.CanRead(ss: ss, site: true) && checkPermission)
                        {
                            where.CheckRecordPermission(ss);
                        }
                    }
                }
            }
            return where;
        }

        public static SqlWhereCollection CanRead(
            this SqlWhereCollection where,
            Context context,
            string idColumnBracket)
        {
            return context.HasPrivilege
                ? where
                : where
                    .Sites_TenantId(context.TenantId)
                    .Or(or: new SqlWhereCollection()
                        .Add(
                            tableName: null,
                            raw: Def.Sql.CanReadSites)
                        .Add(
                            tableName: null,
                            subLeft: CheckRecordPermission(idColumnBracket),
                            _operator: null));
        }

        private static SqlWhereCollection CheckRecordPermission(
            this SqlWhereCollection where, SiteSettings ss, List<long> siteIdList = null)
        {
            return where.Add(
                tableName: ss.ReferenceType,
                subLeft: CheckRecordPermission(ss.IdColumnBracket(), siteIdList),
                _operator: null);
        }

        public static SqlExists CheckRecordPermission(
            string idColumnBracket, List<long> siteIdList = null)
        {
            return Rds.ExistsPermissions(
                where: Rds.PermissionsWhere()
                    .ReferenceId(raw: idColumnBracket)
                    .ReferenceId(
                        sub: Rds.SelectItems(
                        column: Rds.ItemsColumn().ReferenceId(),
                            where: Rds.ItemsWhere()
                                .ReferenceId(raw: "[Permissions].[ReferenceId]")
                                .SiteId_In(siteIdList)),
                        _using: siteIdList?.Any() == true)
                    .PermissionType(_operator: " & 1 = 1")
                    .Or(Rds.PermissionsWhere()
                        .GroupId_In(sub: Rds.SelectGroupMembers(
                            column: Rds.GroupMembersColumn().GroupId(),
                            where: Rds.GroupMembersWhere()
                                .Add(raw: DeptOrUser("GroupMembers"))))
                        .Add(raw: DeptOrUser("Permissions"))));
        }

        private static string DeptOrUser(string tableName)
        {
            return "((@_D <> 0 and [{0}].[DeptId]=@_D) or(@_U <> 0 and [{0}].[UserId]=@_U))"
                .Params(tableName);
        }

        private static Dictionary<long, Types> Hash(EnumerableRowCollection<DataRow> dataRows)
        {
            var hash = dataRows
                .Select(o => o["ReferenceId"].ToLong())
                .Distinct()
                .ToDictionary(o => o, o => Types.NotSet);
            dataRows.ForEach(dataRow =>
            {
                var key = dataRow["ReferenceId"].ToLong();
                hash[key] |= (Types)dataRow["PermissionType"].ToLong();
            });
            return hash;
        }

        public static Types Get(Context context, long siteId)
        {
            var data = Get(context: context, targets: siteId.ToSingleList());
            return data.Count() == 1
                ? data.First().Value
                : Types.NotSet;
        }

        public static bool Can(Context context, long siteId, Types type)
        {
            return (Get(context: context, siteId: siteId) & type) == type
                || context.HasPrivilege;
        }

        public static bool CanRead(Context context, long siteId)
        {
            return (Get(context: context, siteId: siteId) & Types.Read) == Types.Read
                || context.HasPrivilege;
        }

        public static long InheritPermission(Context context, long id)
        {
            return Rds.ExecuteScalar_long(
                context: context,
                statements: Rds.SelectSites(
                    column: Rds.SitesColumn().InheritPermission(),
                    where: Rds.SitesWhere()
                        .SiteId(sub: Rds.SelectItems(
                            column: Rds.ItemsColumn().SiteId(),
                            where: Rds.ItemsWhere().ReferenceId(id)))));
        }

        public static IEnumerable<long> AllowSites(
            Context context, IEnumerable<long> sites, string referenceType = null)
        {
            return Rds.ExecuteTable(
                context: context,
                statements: Rds.SelectSites(
                    column: Rds.SitesColumn().SiteId(),
                    where: Rds.SitesWhere()
                        .TenantId(context.TenantId)
                        .SiteId_In(sites)
                        .ReferenceType(referenceType, _using: referenceType != null)
                        .Add(
                            raw: Def.Sql.CanReadSites,
                            _using: !context.HasPrivilege)))
                                .AsEnumerable()
                                .Select(o => o["SiteId"].ToLong());
        }

        public static IEnumerable<Column> AllowedColumns(
            this IEnumerable<Column> columns,
            bool checkPermission,
            IEnumerable<ColumnAccessControl> readColumnAccessControls)
        {
            return columns
                .Where(o => !checkPermission || o.CanRead || readColumnAccessControls?.Any(p =>
                    p.ColumnName == o.ColumnName && p.AllowedUsers?.Any() == true) == true);
        }

        public static IEnumerable<string> AllowedColumns(SiteSettings ss)
        {
            return ss.Columns.AllowedColumns(
                checkPermission: true,
                readColumnAccessControls: ss.ReadColumnAccessControls)
                    .Select(o => o.ColumnName)
                    .ToList();
        }

        public static bool Allowed(
            this List<ColumnAccessControl> columnAccessControls,
            Column column,
            Types? type,
            List<string> mine)
        {
            return columnAccessControls?
                .FirstOrDefault(o => o.ColumnName == column.ColumnName)?
                .Allowed(type, mine) != false;
        }

        public static bool HasPermission(this Context context, SiteSettings ss)
        {
            return ss.PermissionType != null
                || ss.ItemPermissionType != null
                || context.HasPrivilege;
        }

        public static bool CanRead(this Context context, SiteSettings ss, bool site = false)
        {
            switch (context.Controller)
            {
                case "depts":
                    return CanManageTenant(context: context);
                case "groups":
                    return CanReadGroup(context: context);
                case "users":
                    return CanManageTenant(context: context)
                        || context.UserId == context.Id;
                default:
                    return context.Can(ss: ss, type: Types.Read, site: site);
            }
        }

        public static bool CanCreate(this Context context, SiteSettings ss, bool site = false)
        {
            switch (context.Controller)
            {
                case "depts":
                case "users":
                    return CanManageTenant(context: context);
                case "groups":
                    return CanEditGroup(context: context);
                default:
                    return context.Can(ss: ss, type: Types.Create, site: site);
            }
        }

        public static bool CanUpdate(this Context context, SiteSettings ss, bool site = false)
        {
            switch (context.Controller)
            {
                case "depts":
                    return CanManageTenant(context: context);
                case "groups":
                    return CanEditGroup(context: context);
                case "users":
                    return CanManageTenant(context: context)
                        || context.UserId == context.Id;
                default:
                    if (ss.ReferenceType == "Sites")
                    {
                        return context.CanManageSite(ss: ss);
                    }
                    else
                    {
                        return context.Can(ss: ss, type: Types.Update, site: site);
                    }
            }
        }

        public static bool CanMove(Context context, SiteSettings source, SiteSettings destination)
        {
            return context.CanUpdate(ss: source)
                && context.CanUpdate(ss: destination);
        }

        public static bool CanDelete(this Context context, SiteSettings ss, bool site = false)
        {
            switch (context.Controller)
            {
                case "depts":
                    return CanManageTenant(context: context);
                case "groups":
                    return CanEditGroup(context: context);
                case "users":
                    return CanManageTenant(context: context)
                        && context.UserId != context.Id;
                default:
                    if (ss.ReferenceType == "Sites")
                    {
                        return context.CanManageSite(ss: ss);
                    }
                    else
                    {
                        return context.Can(ss: ss, type: Types.Delete, site: site);
                    }
            }
        }

        public static bool CanSendMail(this Context context, SiteSettings ss, bool site = false)
        {
            if (!Contract.Mail(context: context)) return false;
            switch (context.Controller)
            {
                case "depts":
                    return CanManageTenant(context: context);
                case "groups":
                    return CanEditGroup(context: context);
                case "users":
                    return CanManageTenant(context: context)
                        || context.UserId == context.Id;
                default:
                    if (ss.ReferenceType == "Sites")
                    {
                        return context.CanManageSite(ss: ss);
                    }
                    else
                    {
                        return context.Can(ss: ss, type: Types.SendMail, site: site);
                    }
            }
        }

        public static bool CanImport(this Context context, SiteSettings ss, bool site = false)
        {
            return Contract.Import(context: context)
                && context.Can(ss: ss, type: Types.Import, site: site);
        }

        public static bool CanExport(this Context context, SiteSettings ss, bool site = false)
        {
            return Contract.Export(context: context)
                && context.Can(ss: ss, type: Types.Export, site: site);
        }

        public static bool CanManageSite(this Context context, SiteSettings ss, bool site = false)
        {
            return context.Can(ss: ss, type: Types.ManageSite, site: site);
        }

        public static bool CanManagePermission(this Context context, SiteSettings ss, bool site = false)
        {
            return context.Can(ss: ss, type: Types.ManagePermission, site: site);
        }

        public static ColumnPermissionTypes ColumnPermissionType(this Column self, Context context)
        {
            switch(context.Action)
            {
                case "new":
                    return self.CanCreate
                        ? ColumnPermissionTypes.Update
                        : self.CanRead
                            ? ColumnPermissionTypes.Read
                            : ColumnPermissionTypes.Deny;
                default:
                    return self.CanRead && self.CanUpdate
                        ? ColumnPermissionTypes.Update
                        : self.CanRead
                            ? ColumnPermissionTypes.Read
                            : ColumnPermissionTypes.Deny;
            }
        }

        public static bool CanManageTenant(Context context)
        {
            return context.User.TenantManager
                || context.HasPrivilege;
        }

        public static bool CanReadGroup(Context context)
        {
            return 
                context.UserSettings.DisableGroupAdmin != true &&
                (context.Id == 0 ||
                CanManageTenant(context: context) ||
                Groups(context: context).Any() ||
                context.HasPrivilege);
        }

        public static bool CanEditGroup(Context context)
        {
            return
                context.UserSettings.DisableGroupAdmin != true
                && (context.Id == 0
                || CanManageTenant(context: context)
                || Groups(context: context).Any(o => o["Admin"].ToBool())
                || context.HasPrivilege);
        }

        private static bool Can(this Context context, SiteSettings ss, Types type, bool site)
        {
            return (ss.GetPermissionType(site) & type) == type
                || context.HasPrivilege;
        }

        private static EnumerableRowCollection<DataRow> Groups(Context context)
        {
            return Rds.ExecuteTable(
                context: context,
                statements: Rds.SelectGroupMembers(
                    column: Rds.GroupMembersColumn().Admin(),
                    where: Rds.GroupMembersWhere()
                        .GroupId(context.Id)
                        .Add(raw: DeptOrUser("GroupMembers"))))
                            .AsEnumerable();
        }

        public static SqlWhereCollection GroupMembersWhere()
        {
            return Rds.GroupMembersWhere().Add(raw: DeptOrUser("GroupMembers"));
        }

        public static Types? Admins(Context context, Types? type = Types.NotSet)
        {
            if (context.User.TenantManager) type |= Types.ManageTenant;
            if (context.User.ServiceManager) type |= Types.ManageService;
            return type;
        }
    }
}