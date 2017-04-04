using System;
using System.Collections.Specialized;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using Version = Sitecore.Data.Version;

namespace Sitecore.Support.Shell.Framework.Commands
{
  [Serializable]
  public class ResetSortorder : Command
  {
    public override void Execute(CommandContext context)
    {
      Error.AssertObject(context, "context");
      if (context.Items.Length != 1) return;

      var item = context.Items[0];
      var parameters = new NameValueCollection
      {
        ["id"] = item.ID.ToString(),
        ["language"] = item.Language.ToString(),
        ["version"] = item.Version.ToString()
      };

      Context.ClientPage.Start(this, "Run", parameters);
    }

    public override CommandState QueryState(CommandContext context)
    {
      Error.AssertObject(context, "context");
      if (context.Items.Length != 1)
        return CommandState.Disabled;
      var item = context.Items[0];
      if (!item.HasChildren)
        return CommandState.Disabled;
      if (item.Appearance.ReadOnly)
        return CommandState.Disabled;
      if (IsLockedByOther(item))
        return CommandState.Disabled;
      return !CanWriteField(item, FieldIDs.Sortorder) ? CommandState.Disabled : base.QueryState(context);
    }

    protected void Run(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (args.Result != "yes") return;

        var str = args.Parameters["id"];
        var name = args.Parameters["language"];
        var str3 = args.Parameters["version"];
        var item = Context.ContentDatabase.Items[str, Language.Parse(name), Version.Parse(str3)];

        Error.AssertItemFound(item);
        SortReset(item);
        Log.Audit(this, "Reset sort order: {0}", AuditFormatter.FormatItem(item));

        Context.ClientPage.SendMessage(this, "item:sortorderresetted");
      }
      else
      {
        Context.ClientPage.ClientResponse.Confirm("Do you want to reset the sort order for the subitems?");
        args.WaitForPostBack();
      }
    }

    public static void SortReset(Item parent)
    {
      Error.AssertObject(parent, "parent");
      var children = parent.Children;
      var str = Settings.DefaultSortOrder.ToString();
      foreach (Item item in children)
      {
        if (item == null || !item.Access.CanWrite() || item[FieldIDs.Sortorder] == str) continue;

        var count = item.Versions.Count;
        item.Editing.BeginEdit();
        item[FieldIDs.Sortorder] = str;
        item.Editing.EndEdit();

        if (count == 0) item.Versions.RemoveAll(false);
      }
    }
  }
}