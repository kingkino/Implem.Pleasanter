﻿using Implem.DefinitionAccessor;
using Implem.Pleasanter.Libraries.General;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Responses;
using Implem.Pleasanter.Libraries.Security;
using Implem.Pleasanter.Libraries.Server;
using Implem.Pleasanter.Libraries.Settings;
using System.Collections.Generic;
using System.Linq;
namespace Implem.Pleasanter.Libraries.HtmlParts
{
    public static class HtmlNavigationMenu
    {
        public static HtmlBuilder NavigationMenu(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            long siteId,
            string referenceType,
            Error.Types errorType,
            bool useNavigationMenu,
            bool useSearch)
        {
            return errorType == Error.Types.None && useNavigationMenu
                ? hb.Nav(
                    id: "Navigations",
                    css: "ui-widget-header",
                    action: () => hb
                        .NavigationMenu(
                            context: context,
                            ss: ss,
                            siteId: siteId,
                            referenceType: referenceType,
                            useNavigationMenu: useNavigationMenu)
                        .Search(_using: useSearch))
                : hb;
        }

        private static HtmlBuilder NavigationMenu(
            this HtmlBuilder hb,
            Context context,
            SiteSettings ss,
            long siteId,
            string referenceType,
            bool useNavigationMenu)
        {
            var canManageGroups = Sessions.UserSettings().DisableGroupAdmin != true;
            var canManageSite = siteId != 0 && context.CanManageSite(ss: ss, site: true);
            var canManageDepts = Permissions.CanManageTenant(context: context);
            var canManageUsers = Permissions.CanManageTenant(context: context);
            var canManageTrashBox = CanManageTrashBox(context: context, ss: ss);
            return hb.Ul(
                id: "NavigationMenu",
                action: () => hb
                    .Li(
                        action: () => hb
                            .Div(action: () => hb
                                .A(
                                    href: NewHref(context: context, ss: ss),
                                    attributes: SiteIndex(context: context, ss: ss)
                                        ? new HtmlAttributes()
                                            .OnClick("$p.templates($(this));")
                                            .DataAction("Templates")
                                            .DataMethod("post")
                                        : null,
                                    action: () => hb
                                        .Span(css: "ui-icon ui-icon-plus")
                                        .Text(text: Displays.New()))),
                        _using: ss.ReferenceType == "Sites" && context.Action == "index"
                            ? context.CanManageSite(ss: ss)
                            : context.CanCreate(ss: ss)
                                && ss.ReferenceType != "Wikis"
                                && context.Action != "trashbox")
                    .Li(
                        css: "sub-menu",
                        action: () => hb
                            .Div(
                                attributes: new HtmlAttributes().DataId("ViewModeMenu"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-triangle-1-e")
                                    .Text(text: Displays.View()))
                            .ViewModeMenu(context: context, ss: ss),
                        _using: Def.ViewModeDefinitionCollection
                            .Any(o => o.ReferenceType == referenceType))
                    .Li(
                        css: "sub-menu",
                        action: () => hb
                            .Div(
                                attributes: new HtmlAttributes().DataId("SettingsMenu"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-gear")
                                    .Text(text: Displays.Manage()))
                            .SettingsMenu(
                                ss: ss,
                                siteId: siteId,
                                canManageSite: canManageSite,
                                canManageDepts: canManageDepts,
                                canManageGroups: canManageGroups,
                                canManageUsers: canManageUsers,
                                canManageTrashBox: canManageTrashBox),
                        _using:
                            canManageSite ||
                            canManageDepts ||
                            canManageGroups ||
                            canManageUsers)
                    .Li(
                        css: "sub-menu",
                        action: () => hb
                            .Div(
                                attributes: new HtmlAttributes().DataId("AccountMenu"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-person")
                                    .Text(text: SiteInfo.UserName(
                                        context: context,
                                        userId: context.UserId)))
                            .AccountMenu(context: context)));

        }

        private static string NewHref(Context context, SiteSettings ss)
        {
            switch (context.Controller)
            {
                case "items":
                    return SiteIndex(context: context, ss: ss)
                        ? "javascript:void(0);"
                        : Locations.ItemNew(ss.SiteId);
                default:
                    return Locations.New(context.Controller);
            }
        }

        private static bool SiteIndex(Context context, SiteSettings ss)
        {
            return ss.ReferenceType == "Sites" && context.Action == "index";
        }

        private static HtmlBuilder ViewModeMenu(
            this HtmlBuilder hb, Context context, SiteSettings ss)
        {
            return hb.Ul(id: "ViewModeMenu", css: "menu", action: () =>
            {
                Def.ViewModeDefinitionCollection
                    .Where(mode => mode.ReferenceType == ss.ReferenceType)
                    .Where(mode => ss.EnableViewMode(
                        context: context, name: mode.Name))
                    .Select(mode => mode.Name)
                    .ForEach(action => hb
                        .ViewModeMenu(
                            siteId: ss.SiteId,
                            referenceType: ss.ReferenceType,
                            action: action,
                            postBack: PostBack(context: context, ss: ss)));
            });
        }

        private static bool PostBack(Context context, SiteSettings ss)
        {
            return new List<string>
            {
                "new",
                "create",
                "edit",
                "copy",
                "move",
                "separate",
                "history"
            }.Contains(context.Action) || ss.Scripts?.Any() == true || ss.Styles.Any() == true;
        }

        private static HtmlBuilder ViewModeMenu(
            this HtmlBuilder hb,
            long siteId,
            string referenceType,
            string action,
            bool postBack)
        {
            return hb.Li(action: () => hb
                .A(
                    attributes: postBack
                        ? new HtmlAttributes().OnClick(
                            "location.href='" + Locations.ItemView(siteId, action) + "'")
                        : new HtmlAttributes()
                            .OnClick("$p.viewMode($(this));")
                            .DataAction(action),
                    action: () => hb
                        .Span(css: "ui-icon ui-icon-triangle-1-e")
                        .Text(text: Displays.Get(action))));
        }

        private static HtmlBuilder SettingsMenu(
            this HtmlBuilder hb,
            SiteSettings ss,
            long siteId,
            bool canManageSite,
            bool canManageDepts,
            bool canManageGroups,
            bool canManageUsers,
            bool canManageTrashBox)
        {
            return hb.Ul(
                id: "SettingsMenu",
                css: "menu",
                action: () => hb
                    .Li(
                        action: () => hb
                            .A(
                                href: Locations.ItemEdit(siteId),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-gear")
                                    .Text(text: SiteSettingsDisplayName(ss))),
                        _using: canManageSite)
                    .Li(
                        action: () => hb
                            .A(
                                href: Locations.Index("Depts"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-gear")
                                    .Text(text: Displays.DeptAdmin())),
                        _using: canManageDepts)
                    .Li(
                        action: () => hb
                            .A(
                                href: Locations.Index("Groups"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-gear")
                                    .Text(text: Displays.GroupAdmin())),
                        _using: canManageGroups)
                    .Li(
                        action: () => hb
                            .A(
                                href: Locations.Index("Users"),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-gear")
                                    .Text(text: Displays.UserAdmin())),
                        _using: canManageUsers)
                    .Li(
                        action: () => hb
                            .A(
                                href: Locations.ItemTrashBox(siteId),
                                action: () => hb
                                    .Span(css: "ui-icon ui-icon-trash")
                                    .Text(text: Displays.TrashBox())),
                        _using: canManageTrashBox));
        }

        private static string SiteSettingsDisplayName(SiteSettings ss)
        {
            switch (ss.ReferenceType)
            {
                case "Sites":
                    return Displays.ManageFolder();
                case "Issues":
                case "Results":
                    return Displays.ManageTable();
                case "Wikis":
                    return Displays.ManageWiki();
                default:
                    return null;
            }
        }

        private static HtmlBuilder AccountMenu(this HtmlBuilder hb, Context context)
        {
            return hb.Ul(id: "AccountMenu", css: "menu", action: () => hb
                .Li(action: () => hb
                    .A(
                        href: Locations.Logout(),
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-locked")
                            .Text(text: Displays.Logout())))
                .Li(
                    action: () => hb
                        .A(
                            href: Locations.Edit("Users", context.UserId),
                            action: () => hb
                                .Span(css: "ui-icon ui-icon-wrench")
                                .Text(text: Displays.EditProfile())),
                    _using: Parameters.Service.ShowProfiles)
                .Li(
                    action: () => hb
                        .A(
                            href: Locations.Get("Users", "EditApi"),
                            action: () => hb
                                .Span(css: "ui-icon ui-icon-link")
                                .Text(text: Displays.ApiSettings())),
                    _using: Contract.Api(context: context))
                .Li(action: () => hb
                    .A(
                        href: Parameters.General.HtmlUsageGuideUrl,
                        target: "_blank",
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-help")
                            .Text(text: Displays.UsageGuide())))
                .Li(action: () => hb
                    .A(
                        href: Parameters.General.HtmlBlogUrl,
                        target: "_blank",
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-info")
                            .Text(text: Displays.Blog())))
                .Li(action: () => hb
                    .A(
                        href: Parameters.General.HtmlSupportUrl,
                        target: "_blank",
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-contact")
                            .Text(text: Displays.Support())))
                .Li(action: () => hb
                    .A(
                        href: Parameters.General.HtmlContactUrl,
                        target: "_blank",
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-contact")
                            .Text(text: Displays.Contact())))
                .Li(action: () => hb
                    .A(
                        href: Parameters.General.HtmlPortalUrl,
                        target: "_blank",
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-cart")
                            .Text(text: Displays.Portal())))
                .Li(action: () => hb
                    .A(
                        href: Locations.Get("versions"),
                        action: () => hb
                            .Span(css: "ui-icon ui-icon-info")
                            .Text(text: Displays.Version()))));
        }

        private static HtmlBuilder Search(this HtmlBuilder hb, bool _using)
        {
            return _using
                ? hb
                    .Div(id: "SearchField", action: () => hb
                        .TextBox(
                            controlId: "Search",
                            controlCss: " w150 redirect",
                            placeholder: Displays.Search()))
                : hb;
        }

        private static bool CanManageTrashBox(Context context, SiteSettings ss)
        {
            return (Parameters.Deleted.Restore || Parameters.Deleted.PhysicalDelete)
                && context.Controller == "items"
                && context.CanManageSite(ss: ss)
                && (context.Id != 0 || context.HasPrivilege);
        }
    }
}