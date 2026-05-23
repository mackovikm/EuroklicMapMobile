using Android.Widget;

namespace EuroklicMapMobile.Platforms.Android;

/// <summary>
/// Long-click listener pro Label se třídou CopyOnLongPress.
/// Zkopíruje text do schránky, zobrazí krátký toast a vrátí true
/// (potlačí systémový dialog výběru textu).
/// </summary>
internal class CopyLongClickListener : Java.Lang.Object, global::Android.Views.View.IOnLongClickListener
{
    private readonly Microsoft.Maui.Controls.Label _label;

    public CopyLongClickListener(Microsoft.Maui.Controls.Label label) => _label = label;

    public bool OnLongClick(global::Android.Views.View? v)
    {
        var text = _label.Text;
        if (string.IsNullOrEmpty(text)) return true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Clipboard.Default.SetTextAsync(text);
            Toast.MakeText(
                global::Android.App.Application.Context,
                "Adresa zkopírována do schránky",
                ToastLength.Short)?.Show();
        });

        return true; // true = event consumed → systémový dialog se nezobrazí
    }
}

/// <summary>
/// Prázdný ActionMode callback – potlačí toolbar pro výběr/kopírování textu.
/// </summary>
internal class NoOpActionModeCallback : Java.Lang.Object, global::Android.Views.ActionMode.ICallback
{
    public bool OnCreateActionMode(global::Android.Views.ActionMode? mode, global::Android.Views.IMenu? menu) => false;
    public bool OnPrepareActionMode(global::Android.Views.ActionMode? mode, global::Android.Views.IMenu? menu) => false;
    public bool OnActionItemClicked(global::Android.Views.ActionMode? mode, global::Android.Views.IMenuItem? item) => false;
    public void OnDestroyActionMode(global::Android.Views.ActionMode? mode) { }
}
