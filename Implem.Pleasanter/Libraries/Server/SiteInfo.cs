﻿using Implem.Libraries.DataSources.SqlServer;
using Implem.Libraries.Utilities;
using Implem.Pleasanter.Libraries.DataSources;
using Implem.Pleasanter.Libraries.DataTypes;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Responses;
using Implem.Pleasanter.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Implem.Pleasanter.Libraries.Server
{
    public static class SiteInfo
    {
        public static Dictionary<int, TenantCache> TenantCaches = new Dictionary<int, TenantCache>();

        public static void Reflesh(Context context, bool force = false)
        {
            var tenantCache = TenantCaches.Get(context.TenantId);
            var monitor = tenantCache.GetUpdateMonitor(context: context);
            if (monitor.DeptsUpdated || monitor.UsersUpdated || force)
            {
                var dataSet = Rds.ExecuteDataSet(
                    context: context,
                    statements: new SqlStatement[]
                    {
                        Rds.SelectDepts(
                            dataTableName: "Depts",
                            column: Rds.DeptsColumn()
                                .TenantId()
                                .DeptId()
                                .DeptCode()
                                .DeptName(),
                            where: Rds.DeptsWhere().TenantId(context.TenantId),
                            _using: monitor.DeptsUpdated || force),
                        Rds.SelectUsers(
                            dataTableName: "Users",
                            column: Rds.UsersColumn()
                                .TenantId()
                                .UserId()
                                .DeptId()
                                .LoginId()
                                .Name()
                                .TenantManager()
                                .ServiceManager()
                                .Disabled(),
                            where: Rds.UsersWhere().TenantId(context.TenantId),
                            _using: monitor.UsersUpdated || force)
                    });
                if (monitor.DeptsUpdated || force)
                {
                    tenantCache.DeptHash = dataSet.Tables["Depts"]
                        .AsEnumerable()
                        .ToDictionary(
                            dataRow => dataRow.Int("DeptId"),
                            dataRow => new Dept(dataRow));
                }
                if (monitor.UsersUpdated || force)
                {
                    tenantCache.UserHash = dataSet.Tables["Users"]
                        .AsEnumerable()
                        .ToDictionary(
                            dataRow => dataRow.Int("UserId"),
                            dataRow => new User(dataRow));
                }
            }
            if (monitor.PermissionsUpdated || monitor.GroupsUpdated || monitor.UsersUpdated || force)
            {
                tenantCache.SiteUserHash = new Dictionary<long, List<int>>();
            }
            if (monitor.SitesUpdated || force)
            {
                tenantCache.SiteMenu = new SiteMenu(context: context);
            }
            if (monitor.Updated || force)
            {
                monitor.Update();
            }
        }

        public static IEnumerable<int> SiteUsers(Context context, long siteId)
        {
            var tenantCache = TenantCaches.Get(context.TenantId);
            if (!tenantCache.SiteUserHash.ContainsKey(siteId))
            {
                SetSiteUserHash(
                    context: context,
                    siteId: siteId,
                    reload: true);
            }
            return tenantCache.SiteUserHash.Get(siteId);
        }

        public static void SetSiteUserHash(Context context, long siteId, bool reload = false)
        {
            var tenantCache = TenantCaches.Get(context.TenantId);
            if (!tenantCache.SiteUserHash.ContainsKey(siteId))
            {
                try
                {
                    tenantCache.SiteUserHash.Add(siteId, GetSiteUserHash(
                        context: context,
                        siteId: siteId));
                }
                catch (Exception)
                {
                }
            }
            else if (reload)
            {
                tenantCache.SiteUserHash[siteId] = GetSiteUserHash(
                    context: context,
                    siteId: siteId);
            }
        }

        private static List<int> GetSiteUserHash(Context context, long siteId)
        {
            var siteUserCollection = new List<int>();
            foreach (DataRow dataRow in SiteUserDataTable(
                context: context,
                siteId: siteId).Rows)
            {
                siteUserCollection.Add(dataRow["UserId"].ToInt());
            }
            return siteUserCollection;
        }

        private static DataTable SiteUserDataTable(Context context, long siteId)
        {
            var deptRaw = "[Users].[DeptId] and [Users].[DeptId]>0";
            var userRaw = "[Users].[UserId] and [Users].[UserId]>0";
            return Rds.ExecuteTable(
                context: context,
                statements: Rds.SelectUsers(
                    distinct: true,
                    column: Rds.UsersColumn().UserId(),
                    where: Rds.UsersWhere()
                        .TenantId(context.TenantId)
                        .Add(
                            subLeft: Rds.SelectPermissions(
                                column: Rds.PermissionsColumn()
                                    .PermissionType(function: Sqls.Functions.Max),
                                where: Rds.PermissionsWhere()
                                    .ReferenceId(siteId)
                                    .Or(Rds.PermissionsWhere()
                                        .DeptId(raw: deptRaw)
                                        .Add(
                                            subLeft: Rds.SelectGroupMembers(
                                                column: Rds.GroupMembersColumn()
                                                    .GroupMembersCount(),
                                                where: Rds.GroupMembersWhere()
                                                    .GroupId(raw: "[Permissions].[GroupId]")
                                                    .Or(Rds.GroupMembersWhere()
                                                        .DeptId(raw: deptRaw)
                                                        .UserId(raw: userRaw))
                                                    .Add(raw: "[Permissions].[GroupId]>0")),
                                            _operator: ">0")
                                        .UserId(raw: userRaw))),
                            _operator: ">0")));
        }

        public static Dept Dept(int tenantId, int deptId)
        {
            return TenantCaches.Get(tenantId)?.DeptHash?
                .Where(o => o.Key == deptId)
                .Select(o => o.Value)
                .FirstOrDefault();
        }

        public static User User(Context context, int userId)
        {
            return TenantCaches.Get(context.TenantId)?.UserHash?
                .Where(o => o.Key == userId)
                .Select(o => o.Value)
                .FirstOrDefault() ?? Anonymous(context: context);
        }

        private static User Anonymous(Context context)
        {
            return new User(
                context: context,
                userId: DataTypes.User.UserTypes.Anonymous.ToInt());
        }

        public static string UserName(Context context, int userId, bool notSet = true)
        {
            var name = User(
                context: context,
                userId: userId).Name;
            return name != null
                ? name
                : notSet
                    ? Displays.NotSet()
                    : string.Empty;
        }
    }
}