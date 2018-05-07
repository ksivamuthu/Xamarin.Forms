﻿using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V4.Widget;
using Android.Support.V7.Graphics.Drawable;
using Android.Views;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using ActionBarDrawerToggle = Android.Support.V7.App.ActionBarDrawerToggle;
using AView = Android.Views.View;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using LP = Android.Views.ViewGroup.LayoutParams;
using R = Android.Resource;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellToolbarTracker : Java.Lang.Object, AView.IOnClickListener, IShellToolbarTracker
	{
		private bool _canNavigateBack;
		private bool _disposed;
		private DrawerLayout _drawerLayout;
		private ActionBarDrawerToggle _drawerToggle;
		private Page _page;
		private IShellContext _shellContext;
		private Toolbar _toolbar;
		private IShellSearchView _searchView;

		public ShellToolbarTracker(IShellContext shellContext, Toolbar toolbar, DrawerLayout drawerLayout)
		{
			_shellContext = shellContext ?? throw new ArgumentNullException(nameof(shellContext));
			_toolbar = toolbar ?? throw new ArgumentNullException(nameof(toolbar));
			_drawerLayout = drawerLayout ?? throw new ArgumentNullException(nameof(drawerLayout));
		}

		public bool CanNavigateBack
		{
			get { return _canNavigateBack; }
			set
			{
				if (_canNavigateBack == value)
					return;
				_canNavigateBack = value;
				UpdateLeftBarButtonItem();
			}
		}

		public Page Page
		{
			get { return _page; }
			set
			{
				if (_page == value)
					return;
				var oldPage = _page;
				_page = value;
				OnPageChanged(oldPage, _page);
			}
		}

		void AView.IOnClickListener.OnClick(AView v)
		{
			OnNavigateBack();
		}

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_drawerToggle.Dispose();
				}

				_shellContext = null;
				_drawerToggle = null;
				Page = null;
				_toolbar = null;
				_drawerLayout = null;
				_disposed = true;
			}
		}

		protected virtual void OnNavigateBack()
		{
			Page.Navigation.PopAsync();
		}

		protected virtual void OnPageChanged(Page oldPage, Page newPage)
		{
			if (oldPage != null)
			{
				oldPage.PropertyChanged -= OnPagePropertyChanged;
				((INotifyCollectionChanged)oldPage.ToolbarItems).CollectionChanged -= OnPageToolbarItemsChanged;
			}

			if (newPage != null)
			{
				newPage.PropertyChanged += OnPagePropertyChanged;
				((INotifyCollectionChanged)newPage.ToolbarItems).CollectionChanged += OnPageToolbarItemsChanged;

				UpdatePageTitle(_toolbar, newPage);
				UpdateLeftBarButtonItem();
				UpdateToolbarItems();
			}
		}

		protected virtual void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Page.TitleProperty.PropertyName)
			{
				UpdatePageTitle(_toolbar, Page);
			}
		}

		protected virtual void OnPageToolbarItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateToolbarItems(_toolbar, Page);
		}

		protected virtual void UpdateLeftBarButtonItem(Context context, Toolbar toolbar, DrawerLayout drawerLayout, Page page)
		{
			var activity = (FormsAppCompatActivity)context;
			if (_drawerToggle == null)
			{
				_drawerToggle = new ActionBarDrawerToggle((Activity)context, drawerLayout, toolbar,
					R.String.Ok, R.String.Ok)
				{
					ToolbarNavigationClickListener = this,
				};
				drawerLayout.AddDrawerListener(_drawerToggle);
			}

			if (CanNavigateBack)
			{
				_drawerToggle.DrawerIndicatorEnabled = false;
				var icon = new DrawerArrowDrawable(activity.SupportActionBar.ThemedContext);
				icon.Progress = 1;
				toolbar.NavigationIcon = icon;
			}
			else
			{
				toolbar.NavigationIcon = null;
				_drawerToggle.DrawerIndicatorEnabled = true;
			}
			_drawerToggle.SyncState();
		}

		protected virtual void UpdateMenuItemIcon(Context context, IMenuItem menuItem, ToolbarItem toolBarItem)
		{
			FileImageSource icon = toolBarItem.Icon;
			if (!string.IsNullOrEmpty(icon))
			{
				Drawable iconDrawable = context.GetFormsDrawable(icon);
				if (iconDrawable != null)
				{
					menuItem.SetIcon(iconDrawable);
					iconDrawable.Dispose();
				}
			}
		}

		protected virtual void UpdatePageTitle(Toolbar toolbar, Page page)
		{
			_toolbar.Title = page.Title;
		}

		protected virtual IShellSearchView GetSearchView (Context context)
		{
			return new ShellSearchView(context, _shellContext);
		}

		protected virtual void UpdateToolbarItems(Toolbar toolbar, Page page)
		{
			var menu = toolbar.Menu;
			menu.Clear();

			foreach (var item in page.ToolbarItems)
			{
				var menuitem = menu.Add(item.Text);
				UpdateMenuItemIcon(_shellContext.AndroidContext, menuitem, item);
				menuitem.SetEnabled(item.IsEnabled);
				menuitem.SetShowAsAction(ShowAsAction.Always);
				menuitem.SetOnMenuItemClickListener(new GenericMenuClickListener(item.Activate));
			}

			var searchHandler = Shell.GetSearchHandler(page);
			if (searchHandler != null)
			{
				var item = menu.Add(new Java.Lang.String(searchHandler.Placeholder));
				item.SetIcon(Resource.Drawable.abc_ic_search_api_material);
				item.SetShowAsAction(ShowAsAction.IfRoom | ShowAsAction.CollapseActionView);
				var context = _shellContext.AndroidContext;

				var searchView = _searchView = GetSearchView(context);
				searchView.SearchHandler = searchHandler;

				searchView.LoadView();

				searchView.View.LayoutParameters = new LP(LP.MatchParent, LP.MatchParent);
				item.SetActionView(searchView.View);

				searchView.SearchConfirmed += OnSearchConfirmed;
			}
		}

		protected virtual void OnSearchConfirmed(object sender, EventArgs e)
		{
			_toolbar.CollapseActionView();
		}

		private void UpdateLeftBarButtonItem()
		{
			UpdateLeftBarButtonItem(_shellContext.AndroidContext, _toolbar, _drawerLayout, Page);
		}

		private void UpdateToolbarItems()
		{
			UpdateToolbarItems(_toolbar, Page);
		}
	}
}