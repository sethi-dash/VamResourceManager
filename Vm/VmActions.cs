﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Vrm.Cfg;
using Vrm.Json;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Vam;
using Vrm.Window;

namespace Vrm.Vm
{
    public class VmActions : VmBase
    {
        public ICommand CmdLocateVar {get;}
        public ICommand CmdLocateUserItem {get;}
        public ICommand CmdFilterToCreator {get;}
        public ICommand CmdFilterToName {get;}
        public ICommand CmdFilterToVar {get;}
        public ICommand CmdBrowseVar {get;}
        public ICommand CmdSetFavHideTag {get;}
        public ICommand CmdRemove {get;}

        private List<object> _items;
        public List<object> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items))
                    return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public VmBase OwnerTab {get;set;}

        private void Update()
        {
            var cmds = new List<object>();
            var mainVm = OwnerTab.FindMainVm();
            if (SelectedItem != null)
            {              
                cmds.Add(new VmCmdBtn("Remove", CmdRemove));
                if (SelectedItem.IsVar)
                {
                    cmds.Add(new VmCmdBtn("Open in File Explorer", CmdLocateVar));
                    cmds.Add(new VmCmdBtn("Filter to creator", CmdFilterToCreator));
                    cmds.Add(new VmCmdBtn("Filter to package name", CmdFilterToName));
                    cmds.Add(new VmCmdBtn("Filter to var", CmdFilterToVar));
                    cmds.Add(new VmCmdBtn("Browse var", CmdBrowseVar));
                }
                else if (SelectedItem.IsUserItem)
                {
                    cmds.Add(new VmCmdBtn("Open in File Explorer", CmdLocateUserItem));
                }

                if (mainVm.SelectionMode || (!SelectedItem.IsVarSelf && (SelectedItem.IsSceneOrSubscene || SelectedItem.IsClothingOrHair)))
                {
                    cmds.Add(new VmCmdBtn("Change fav/hide/tag..", CmdSetFavHideTag));
                }
            }

            Items = cmds.Any() ? cmds : new List<object> { "<No actions available>" };
        }

        public override void OnShow()
        {
            base.OnShow();

            Update();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnSelectedItemChanged()
        {
            if(IsSelected)
                Update();
        }

        public VmActions()
        {
            Name = "Actions";

            CmdLocateVar = new RelayCommand(_ =>
            {
                try
                {
                    FileHelper.ShowInExplorer(SelectedItem.Var.Info.FullName);
                }
                catch
                {
                    /**/
                }
            }, _=>SelectedItem != null && SelectedItem.IsVar);

            CmdLocateUserItem = new RelayCommand(_ =>
            {
                try
                {
                    FileHelper.ShowInExplorer(SelectedItem.UserItem.Files.First().FullName);
                }
                catch
                {
                    /**/
                }
            }, _=>SelectedItem != null && SelectedItem.IsUserItem);

            CmdFilterToCreator = new RelayCommand(_ =>
            {
                var vm = OwnerTab.FindMainVm();
                if (vm != null)
                    vm.CreatorFilter = SelectedItem.Var.Name.Creator;
            }, _=>SelectedItem != null && SelectedItem.IsVar);

            CmdFilterToName = new RelayCommand(_ =>
            {
                var vm = OwnerTab.FindMainVm();
                if (vm != null)
                    vm.NameFilter = SelectedItem.Var.Name.Name;
            }, _=>SelectedItem != null && SelectedItem.IsVar);

            CmdFilterToVar = new RelayCommand(_ =>
            {
                var vm = OwnerTab.FindMainVm();
                if (vm != null)
                {
                    vm.FilterToVar(SelectedItem.Var.Name);
                }
            },_=>SelectedItem != null && SelectedItem.IsVar);

            CmdBrowseVar = new RelayCommand(_ =>
            {
                var vm = OwnerTab.FindMainVm();
                if (vm != null)
                {
                    vm.BrowseVar(SelectedItem.Var.Name);
                }
            },_=>SelectedItem != null && SelectedItem.IsVar);

            CmdSetFavHideTag = new RelayCommand(x =>
            {
                var mainVm = OwnerTab.FindMainVm();
                var items = mainVm.SelectionMode ? mainVm.GetCheckedElements() : new [] { SelectedItem };
                var w = new FavHideTagWindow();
                foreach (var item in items)
                {
                    if (item.IsClothingOrHair && item.ElementInfo.Exts.Contains(Ext.Vam))
                    {
                        w.Vm.CountHide++;
                        w.Vm.CountTag++;
                    }
                    else if (item.IsSceneOrSubscene)
                    {
                        w.Vm.CountHide++;
                        w.Vm.CountFav++;
                    }
                }
                w.Vm.Invalidate();
                var res = w.ShowDialog();
                if (res.HasValue && res.Value)
                {
                    bool processTag = w.Vm.ChangeTag && w.Vm.CountTag > 0 && !string.IsNullOrWhiteSpace(w.Vm.Tag);
                    bool processFav = w.Vm.ChangeFav && w.Vm.CountFav > 0;
                    bool processHide = w.Vm.ChangeHide && w.Vm.CountHide > 0;
                    foreach (var item in items)
                    {
                        if (processTag && item.IsClothingOrHair)
                        {
                            var fn = item.IsVar 
                                ? FileHelper.GetPrefsFileName_InsideVar(item.RelativePath) 
                                : FileHelper.GetPrefsFileName_UserResource(item.RelativePath);
                            FileHelper.SetTag(fn, w.Vm.Tag);
                        }

                        if (processFav && item.IsSceneOrSubscene)
                        {
                            if (item.IsVar)
                            {
                                var path = FileHelper.ChangeExt(item.FullName, Ext.Json);
                                FileHelper.SetHideOrFav_Scene_InsideVar(path, item.Var.Name, false, w.Vm.IsFav);
                            }
                            else
                            {
                                FileHelper.SetHideOrFav_UserResource(item.UserItem.Item.FullName, false, w.Vm.IsFav);
                                FileHelper.SetFavHide(item.UserItem.Item.FullName, item.ElementInfo);
                            }
                        }

                        if (processHide)
                        {
                            if (item.IsVar)
                            {
                                if (item.IsSceneOrSubscene)
                                {
                                    var path = FileHelper.ChangeExt(item.RelativePath, Ext.Json);
                                    FileHelper.SetHideOrFav_Scene_InsideVar(path, item.Var.Name, true, w.Vm.IsHide);
                                }
                                if (item.IsClothingOrHair)
                                {
                                    var path = FileHelper.ChangeExt(item.RelativePath, Ext.Vam);
                                    FileHelper.SetHide_ClothingOrHair_InsideVar(path, item.Var.Name, w.Vm.IsHide);
                                }
                            }
                            else
                            {
                                FileHelper.SetHideOrFav_UserResource(item.UserItem.Item.FullName, true, w.Vm.IsHide);
                                FileHelper.SetFavHide(item.UserItem.Item.FullName, item.ElementInfo);
                            }
                        }

                        if (item.IsVar)
                        {
                            var v = item.Var;
                            if (v.ElementsDict.TryGetValue(item.ElementInfo.Type, out var entries))
                                FileHelper.SetUserPrefsAndFavHide_InsideVar_Slow(v.Name, entries);
                        }

                        if (item.IsUserItem)
                        {
                            FileHelper.SetFavHide(item.UserItem.Item.FullName, item.UserItem.Info);
                            FileHelper.UpdateTagInElementInfo(item.UserItem.Item.FullName, item.UserItem.Info);
                        }
                    }
                }
            });

            CmdRemove = new RelayCommand(async x =>
            {
                var mainVm = OwnerTab.FindMainVm();
                var items = mainVm.SelectionMode ? mainVm.GetCheckedElements() : new [] { SelectedItem };
                var res = TextBoxDialog.ShowDialog("Confirm Removal", $"Are you sure you want to remove {items.Count()} item(s)?");
                if (!res.HasValue || !res.Value)
                    return;
                await mainVm.Delete(items.ToList());
                mainVm.ToolVar.BrowsingMode = false;
            },_=>
            {
                if (OwnerTab.FindMainVm().SelectionMode)
                    return true;
                else
                    return SelectedItem != null && (SelectedItem.IsVarSelf || SelectedItem.IsUserItem);
            });
        }
    }
}
