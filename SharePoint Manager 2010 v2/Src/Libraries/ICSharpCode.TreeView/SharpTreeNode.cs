﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ICSharpCode.TreeView
{
	public class SharpTreeNode : INotifyPropertyChanged, ISharpTreeNode
	{
		#region Main

		static SharpTreeNode()
		{
			SelectedNodes = new List<SharpTreeNode>();
			ActiveNodes = new List<SharpTreeNode>();			
			StartCuttedDataWatcher();
		}

		public static List<SharpTreeNode> SelectedNodes { get; private set; }
		public static List<SharpTreeNode> ActiveNodes { get; private set; }

		static SharpTreeNode[] ActiveNodesArray
		{
			get
			{
				return ActiveNodes.ToArray();
			}
		}

		public SharpTreeNode()
		{
			Children = new SharpTreeNodeCollection(this);
		}

        [XmlIgnore]
        public SharpTreeNodeCollection Children { get; private set; }

        [XmlIgnore]
		public SharpTreeNode Parent { get; internal set; }

        [XmlIgnore]
        public virtual object Text
		{
			get { return null; }
            set { }
		}

        [XmlIgnore]
        public virtual string IconUri
        {
			get { return null; }
            set { }
		}

        [XmlIgnore]
        public virtual object ToolTip
		{
			get { return null; }
            set { }
		}

        [XmlIgnore]
        public int Level
		{
			get { return Parent != null ? Parent.Level + 1 : 0; }
		}

        [XmlIgnore]
        public bool IsRoot
		{
			get { return Parent == null; }
		}

        //private ObservableCollection<object> _contextMenuItems = null;
        //[Browsable(false)]
        //[XmlIgnore]
        //public virtual ObservableCollection<object> ContextMenuItems
        //{
        //    get
        //    {
        //        if (_contextMenuItems == null)
        //        {
        //            _contextMenuItems = new ObservableCollection<object>();
        //        }
        //        return _contextMenuItems;
        //    }
        //    set
        //    {
        //        _contextMenuItems = value;
        //    }

        //}

		//bool isSelected;

		//public bool IsSelected
		//{
		//    get { return isSelected; }
		//    set
		//    {
		//        isSelected = value;
		//        RaisePropertyChanged("IsSelected");
		//    }
		//}

        //private ContextMenu _contextMenu = null;
        //[Browsable(false)]
        //[XmlIgnore]
        //public virtual ContextMenu ContextMenu
        //{
        //    get 
        //    {
        //        if (_contextMenu == null)
        //        {
        //            _contextMenu = new ContextMenu();
        //        }
        //        return _contextMenu; 
        //    }
        //    set { _contextMenu = value; }
        //}

        public virtual IEnumerable<object> GetContextMenuItems()
        {
            return null;
        }

        public virtual ContextMenu GetContextMenu()
        {
            var menu = new ContextMenu();
            var items = GetContextMenuItems();
            foreach (var item in items.DefaultIfEmpty<object>())
            {
                menu.Items.Add(item);
            }

            return menu;
        }

		internal protected void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
		{
			RaisePropertyChanged("ShowExpander");
			RaiseIsLastChangedIfNeeded(e);
		}

		#endregion

		#region Expanding / LazyLoading

		public event EventHandler Collapsing;

		public virtual string ExpandedIconUri
		{
			get { return this.IconUri; }
		}

		public virtual bool ShowExpander
		{
			get { return Children.Count > 0 || LazyLoading; }
		}

		//public virtual bool ShowLoading
		//{
		//    get { return false; }
		//}

		bool isExpanded;

		public virtual bool IsExpanded
		{
			get { return isExpanded; }
			set
			{
				if (isExpanded != value) {
					isExpanded = value;
					if (isExpanded) {
						EnsureLazyChildren();
					}
					else {
						if (Collapsing != null) {
							Collapsing(this, EventArgs.Empty);
						}
					}
					RaisePropertyChanged("IsExpanded");
				}
			}
		}

		bool lazyLoading;

		public bool LazyLoading
		{
			get { return lazyLoading; }
			set
			{
				lazyLoading = value;
				if (lazyLoading) {
					IsExpanded = false;
				}
				RaisePropertyChanged("LazyLoading");
				RaisePropertyChanged("ShowExpander");
			}
		}


        bool _isSelected;
        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    RaisePropertyChanged("Hans");
                }
            }
        }

		//bool showIcon;
		
		public virtual bool ShowIcon
		{
			get 
            { 
                return !String.IsNullOrEmpty(this.IconUri) || !String.IsNullOrEmpty(this.ExpandedIconUri);
            }
            //set {
            //    showIcon = value;
            //    RaisePropertyChanged("ShowIcon");
            //}
		}

		public virtual void LoadChildren()
		{
		}

		public virtual void EnsureLazyChildren()
		{
			if (LazyLoading) {
                Cursor saveCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;

                LoadChildren();

                Mouse.OverrideCursor = saveCursor;

				LazyLoading = false;
			}
		}

		#endregion

		#region Ancestors / Descendants

		public IEnumerable<SharpTreeNode> Descendants()
		{
			foreach (var child in Children) {
				foreach (var child2 in child.DescendantsAndSelf()) {
					yield return child2;
				}
			}
		}

		public IEnumerable<SharpTreeNode> DescendantsAndSelf()
		{
			yield return this;
			foreach (var child in Descendants()) {
				yield return child;
			}
		}

		public IEnumerable<SharpTreeNode> ExpandedDescendants()
		{
			foreach (var child in Children) {
				foreach (var child2 in child.ExpandedDescendantsAndSelf()) {
					yield return child2;
				}
			}
		}

		public IEnumerable<SharpTreeNode> ExpandedDescendantsAndSelf()
		{
			yield return this;
			if (IsExpanded) {
				foreach (var child in Children) {
					foreach (var child2 in child.ExpandedDescendantsAndSelf()) {
						yield return child2;
					}
				}
			}
		}

		public IEnumerable<SharpTreeNode> Ancestors()
		{
			var node = this;
			while (node.Parent != null) {
				yield return node.Parent;
				node = node.Parent;
			}
		}

		public IEnumerable<SharpTreeNode> AncestorsAndSelf()
		{
			yield return this;
			foreach (var node in Ancestors()) {
				yield return node;
			}
		}

		#endregion

		#region Editing

		public virtual bool IsEditable
		{
			get { return false; }
		}

		bool isEditing;

		public bool IsEditing
		{
			get { return isEditing; }
			set
			{
				if (isEditing != value) {
					isEditing = value;
					RaisePropertyChanged("IsEditing");
				}
			}
		}

		public virtual string LoadEditText()
		{
			return null;
		}

		public virtual bool SaveEditText(string value)
		{
			return true;
		}

		#endregion

		#region Checkboxes

		public virtual bool IsCheckable
		{
			get { return false; }
		}

		bool? isChecked;

		public bool? IsChecked
		{
			get { return isChecked; }
			set
			{
				SetIsChecked(value, true);
			}
		}

		void SetIsChecked(bool? value, bool update)
		{
			if (isChecked != value) {
				isChecked = value;

				if (update) {
					if (IsChecked != null) {
						foreach (var child in Descendants()) {
							if (child.IsCheckable) {
								child.SetIsChecked(IsChecked, false);
							}
						}
					}

					foreach (var parent in Ancestors()) {
						if (parent.IsCheckable) {
							if (!parent.TryValueForIsChecked(true)) {
								if (!parent.TryValueForIsChecked(false)) {
									parent.SetIsChecked(null, false);
								}
							}
						}
					}
				}

				RaisePropertyChanged("IsChecked");
			}
		}

		bool TryValueForIsChecked(bool? value)
		{
			if (Children.Where(n => n.IsCheckable).All(n => n.IsChecked == value)) {
				SetIsChecked(value, false);
				return true;
			}
			return false;
		}

		#endregion

		#region Cut / Copy / Paste / Delete

		static List<SharpTreeNode> cuttedNodes = new List<SharpTreeNode>();
		static IDataObject cuttedData;
		static EventHandler requerySuggestedHandler; // for weak event

		static void StartCuttedDataWatcher()
		{
			requerySuggestedHandler = new EventHandler(CommandManager_RequerySuggested);
			CommandManager.RequerySuggested += requerySuggestedHandler;
		}

		static void CommandManager_RequerySuggested(object sender, EventArgs e)
		{
			if (cuttedData != null && !Clipboard.IsCurrent(cuttedData)) {
				ClearCuttedData();
			}
		}

		static void ClearCuttedData()
		{
			foreach (var node in cuttedNodes) {
				node.IsCut = false;
			}
			cuttedNodes.Clear();
			cuttedData = null;
		}

		//static public IEnumerable<SharpTreeNode> PurifyNodes(IEnumerable<SharpTreeNode> nodes)
		//{
		//    var list = nodes.ToList();
		//    var array = list.ToArray();
		//    foreach (var node1 in array) {
		//        foreach (var node2 in array) {
		//            if (node1.Descendants().Contains(node2)) {
		//                list.Remove(node2);
		//            }
		//        }
		//    }
		//    return list;
		//}

		bool isCut;

		public bool IsCut
		{
			get { return isCut; }
			private set
			{
				isCut = value;
				RaisePropertyChanged("IsCut");
			}
		}

		internal bool InternalCanCut()
		{
			return InternalCanCopy() && InternalCanDelete();
		}

		internal void InternalCut()
		{
			ClearCuttedData();
			cuttedData = Copy(ActiveNodesArray);
			Clipboard.SetDataObject(cuttedData);

			foreach (var node in ActiveNodes) {
				node.IsCut = true;
				cuttedNodes.Add(node);
			}
		}

		internal bool InternalCanCopy()
		{
			return CanCopy(ActiveNodesArray);
		}

		internal void InternalCopy()
		{
			Clipboard.SetDataObject(Copy(ActiveNodesArray));
		}

		internal bool InternalCanPaste()
		{
			return CanPaste(Clipboard.GetDataObject());
		}

		internal void InternalPaste()
		{
			Paste(Clipboard.GetDataObject());

			if (cuttedData != null) {
				DeleteCore(cuttedNodes.ToArray());
				ClearCuttedData();
			}
		}

		internal bool InternalCanDelete()
		{
			return CanDelete(ActiveNodesArray);
		}

		internal void InternalDelete()
		{
			Delete(ActiveNodesArray);
		}		

		public virtual bool CanDelete(SharpTreeNode[] nodes)
		{
			return false;
		}

		public virtual void Delete(SharpTreeNode[] nodes)
		{
		}

		public virtual void DeleteCore(SharpTreeNode[] nodes)
		{
		}

		public virtual bool CanCopy(SharpTreeNode[] nodes)
		{
			return false;
		}

		public virtual IDataObject Copy(SharpTreeNode[] nodes)
		{
			return null;
		}

		public virtual bool CanPaste(IDataObject data)
		{
			return false;
		}

		public virtual void Paste(IDataObject data)
		{
			EnsureLazyChildren();
			Drop(data, Children.Count, DropEffect.Copy);
		}

		#endregion

		#region Drag and Drop

		internal bool InternalCanDrag()
		{
			return CanDrag(ActiveNodesArray);
		}

		internal void InternalDrag(DependencyObject dragSource)
		{
			DragDrop.DoDragDrop(dragSource, Copy(ActiveNodesArray), DragDropEffects.All);
		}

		internal bool InternalCanDrop(DragEventArgs e, int index)
		{
			var finalEffect = GetFinalEffect(e, index);
			e.Effects = GetDragDropEffects(finalEffect);
			return finalEffect != DropEffect.None;
		}

		internal void InternalDrop(DragEventArgs e, int index)
		{
			if (LazyLoading) {
				EnsureLazyChildren();
				index = Children.Count;
			}

			var finalEffect = GetFinalEffect(e, index);
			Drop(e.Data, index, finalEffect);

			if (finalEffect == DropEffect.Move) {
				DeleteCore(ActiveNodesArray);
			}
		}

		DropEffect GetFinalEffect(DragEventArgs e, int index)
		{
			var requestedEffect = GetDropEffect(e);
			var result = CanDrop(e.Data, requestedEffect);
			if (result == DropEffect.Move) {
				if (!CanDelete(ActiveNodesArray)) {
					return DropEffect.None;
				}
			}
			return result;
		}

		static DropEffect GetDropEffect(DragEventArgs e)
		{
			if (e.Data != null) {
				var all = DragDropKeyStates.ControlKey | DragDropKeyStates.ShiftKey | DragDropKeyStates.AltKey;

				if ((e.KeyStates & all) == DragDropKeyStates.ControlKey) {
					return DropEffect.Copy;
				}
				if ((e.KeyStates & all) == DragDropKeyStates.AltKey) {
					return DropEffect.Link;
				}
				if ((e.KeyStates & all) == (DragDropKeyStates.ControlKey | DragDropKeyStates.ShiftKey)) {
					return DropEffect.Link;
				}
				return DropEffect.Move;
			}
			return DropEffect.None;
		}

		static DragDropEffects GetDragDropEffects(DropEffect effect)
		{
			switch (effect) {
				case DropEffect.Copy:
					return DragDropEffects.Copy;
				case DropEffect.Link:
					return DragDropEffects.Link;
				case DropEffect.Move:
					return DragDropEffects.Move;
			}
			return DragDropEffects.None;
		}

		public virtual bool CanDrag(SharpTreeNode[] nodes)
		{
			return false;
		}

		public virtual DropEffect CanDrop(IDataObject data, DropEffect requestedEffect)
		{
			return DropEffect.None;
		}

		public virtual void Drop(IDataObject data, int index, DropEffect finalEffect)
		{
		}

		#endregion

		#region IsLast (for TreeView lines)

		public bool IsLast
		{
			get
			{
				return Parent == null ||
					Parent.Children[Parent.Children.Count - 1] == this;
			}
		}

		void RaiseIsLastChangedIfNeeded(NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					if (e.NewStartingIndex == Children.Count - 1) {
						if (Children.Count > 1) {
							Children[Children.Count - 2].RaisePropertyChanged("IsLast");
						}
						Children[Children.Count - 1].RaisePropertyChanged("IsLast");
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					if (e.OldStartingIndex == Children.Count) {
						if (Children.Count > 0) {
							Children[Children.Count - 1].RaisePropertyChanged("IsLast");
						}
					}
					break;
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		public void RaisePropertyChanged(string name)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}

		#endregion
	}
}
