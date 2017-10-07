﻿using Implem.Pleasanter.Libraries.DataTypes;
using Implem.Pleasanter.Libraries.Html;
using Implem.Pleasanter.Libraries.HtmlParts;
using Implem.Pleasanter.Libraries.Models;
using Implem.Pleasanter.Libraries.Requests;
using Implem.Pleasanter.Libraries.Settings;
using System.Linq;
namespace Implem.Pleasanter.Libraries.Responses
{
    public static class ResponseComment
    {
        public static ResponseCollection PrependComment(
            this ResponseCollection res,
            SiteSettings ss,
            Comments comments,
            Versions.VerTypes verType)
        {
            return Forms.Data("Comments").Trim() != string.Empty
                ? res
                    .Val("#Comments", string.Empty)
                    .Focus("#Comments")
                    .Prepend("#CommentList", new HtmlBuilder()
                        .Comment(
                            ss: ss,
                            comment: comments[0],
                            verType: verType))
                : res;
        }

        public static ResponseCollection Comment(
            this ResponseCollection res,
            SiteSettings ss,
            Comments comments,
            int deleteCommentId)
        {
            comments
                .Where(o => Forms.Exists("Comment" + o.CommentId))
                .ForEach(comment =>
                    res.ReplaceAll(
                        Selector(comment.CommentId),
                        new HtmlBuilder().Comment(
                            ss: ss,
                            comment: comment,
                            verType: Versions.VerTypes.Latest)));
            if (deleteCommentId != 0)
            {
                res
                    .Remove(Selector(deleteCommentId))
                    .Focus("#Comments");
            }
            return res;
        }

        private static string Selector(int commentId)
        {
            return "[id=\"Comment" + commentId + ".wrapper\"]";
        }
    }
}