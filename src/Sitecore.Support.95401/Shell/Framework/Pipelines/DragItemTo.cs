using System;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Pipelines;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
  public class DragItemTo : ItemOperation
  {
    public void Execute(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      var database = GetDatabase(args);
      var source = GetSource(args, database);
      var target = GetTarget(args);
      if (args.Parameters["copy"] == "1")
      {
        Log.Audit(this, "Copy item: {0} to {1}", AuditFormatter.FormatItem(source), AuditFormatter.FormatItem(target));
        SetSortorder(source.CopyTo(target, ItemUtil.GetCopyOfName(target, source.Name)), args);
      }
      else
      {
        Log.Audit(this, "Drag item: {0} to {1}", AuditFormatter.FormatItem(source), AuditFormatter.FormatItem(target));
        source.MoveTo(target);
        SetSortorder(source, args);
      }
    }

    private static Database GetDatabase(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      var database = Factory.GetDatabase(args.Parameters["database"]);
      Error.Assert(database != null, "Database \"" + args.Parameters["database"] + "\" not found.");
      return database;
    }

    private static Item GetSource(ClientPipelineArgs args, Database database)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(database, "database");
      var item = database.GetItem(args.Parameters["id"], Language.Parse(args.Parameters["language"]));
      Assert.IsNotNull(item, typeof(Item), "ID:{0}", args.Parameters["id"]);
      return item;
    }

    private static Item GetTarget(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      var parent = GetDatabase(args).GetItem(ID.Parse(args.Parameters["target"]), Language.Parse(args.Parameters["language"]));
      Assert.IsNotNull(parent, typeof(Item), "ID:{0}", args.Parameters["target"]);
      if (args.Parameters["appendAsChild"] == "1") return parent;
      parent = parent.Parent;
      Assert.IsNotNull(parent, typeof(Item), "ID:{0}.Parent", args.Parameters["target"]);
      return parent;
    }


    private static int Resort(Item target, DragAction dragAction)
    {
      Assert.ArgumentNotNull(target, "target");
      var num = 0;
      var num2 = 0;
      foreach (Item item in target.Parent.Children)
      {
        var count = item.Versions.Count;
        item.Editing.BeginEdit();
        item.Appearance.Sortorder = num2 * 100;
        item.Editing.EndEdit();
        if (count == 0) item.Versions.RemoveAll(false);

        if (item.ID == target.ID)
          num = dragAction == DragAction.Before ? num2 * 100 - 50 : num2 * 100 + 50;
        num2++;
      }
      return num;
    }

    private static void SetItemSortorder(Item item, int sortorder)
    {
      Assert.ArgumentNotNull(item, "item");
      var count = item.Versions.Count;
      item.Editing.BeginEdit();
      item.Appearance.Sortorder = sortorder;
      item.Editing.EndEdit();
      if (count == 0) item.Versions.RemoveAll(false);
    }

    private static void SetSortorder(Item item, ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(args, "args");
      if (args.Parameters["appendAsChild"] == "1") return;
      var target = GetDatabase(args).Items[args.Parameters["target"]];
      if (target == null) return;
      var sortorder = target.Appearance.Sortorder;
      if (args.Parameters["sortAfter"] == "1")
      {
        var nextSibling = target.Axes.GetNextSibling();
        if (nextSibling == null)
        {
          sortorder += 100;
        }
        else
        {
          var num2 = nextSibling.Appearance.Sortorder;
          if (Math.Abs(num2 - sortorder) >= 2)
            sortorder += (num2 - sortorder) / 2;
          else if (target.Parent != null)
            sortorder = Resort(target, DragAction.After);
        }
      }
      else
      {
        var previousSibling = target.Axes.GetPreviousSibling();
        if (previousSibling == null)
        {
          sortorder -= 100;
        }
        else
        {
          var num3 = previousSibling.Appearance.Sortorder;
          if (Math.Abs(num3 - sortorder) >= 2)
            sortorder -= (sortorder - num3) / 2;
          else if (target.Parent != null)
            sortorder = Resort(target, DragAction.Before);
        }
      }
      SetItemSortorder(item, sortorder);
    }
  }
}