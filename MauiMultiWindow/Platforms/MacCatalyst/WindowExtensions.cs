﻿namespace MauiMultiWindow;

using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

public static partial class WindowExtensions
{
	public static Task<T?> OpenModalWindowAsync<T>(this Window parentWindow, ModalWindow<T> content)
	{
		ArgumentNullException.ThrowIfNull(parentWindow.Handler?.MauiContext);
		var taskCompletionSource = new TaskCompletionSource<T?>();
		var parentUIWindow = parentWindow.Handler.PlatformView as UIWindow ?? throw new Exception();

		var modalWindowViewController = new UIViewController();
		var (contentView, size) = GetContentView(content.Content, parentWindow.Handler.MauiContext);
		modalWindowViewController.PreferredContentSize = size;
		modalWindowViewController.View?.AddSubview(contentView);
		var navigationController = new UINavigationController(modalWindowViewController);

		modalWindowViewController.NavigationItem.LeftBarButtonItem = GetActionButton(content.CancelContent, () =>
		{
			parentUIWindow.RootViewController?.DismissViewController(true, taskCompletionSource.SetCanceled);
		});
		modalWindowViewController.NavigationItem.RightBarButtonItem = GetActionButton(content.SubmitContent, async () =>
		{
			var result = await content.SubmitContentAction();
			parentUIWindow.RootViewController?.DismissViewController(
				true, () => taskCompletionSource.SetResult(result));
		});

		parentUIWindow.RootViewController?.PresentViewController(navigationController, true, null);
		return taskCompletionSource.Task;
	}

	private static (UIView, CGSize) GetContentView(VisualElement content, IMauiContext mauiContext)
	{
		var contentView = content.ToPlatform(mauiContext);
		var contentSize = content.Measure(double.PositiveInfinity, double.PositiveInfinity);

		var preferredContentSize = contentSize.ToCGSize() + new CGSize(50, 50);
		contentView.Frame = new CGRect((preferredContentSize.Width - contentSize.Width) / 2,
									   (preferredContentSize.Height - contentSize.Height) / 2,
									   contentSize.Width, contentSize.Height);

		return (contentView, preferredContentSize);
	}

	private static UIBarButtonItem GetActionButton(string content, Action action)
	{
		return new UIBarButtonItem(content, UIBarButtonItemStyle.Plain, (_, _) => action());
	}
}