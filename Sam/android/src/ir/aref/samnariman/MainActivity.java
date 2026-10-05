package ir.aref.samnariman;

import android.app.Activity;
import android.os.Bundle;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;

/** Runs the HTML5 game from the APK's assets in a full-screen, landscape WebView. */
public class MainActivity extends Activity {
    private WebView web;

    @Override
    protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);

        web = new WebView(this);
        web.setBackgroundColor(0xFF140B06);
        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true); // saved progress lives in localStorage
        web.setWebChromeClient(new WebChromeClient());
        web.addJavascriptInterface(new Bridge(), "Android");
        setContentView(web);
        web.loadUrl("file:///android_asset/index.html");
        immersive();
    }

    private void immersive() {
        // IMMERSIVE_STICKY | FULLSCREEN | HIDE_NAVIGATION | LAYOUT_FULLSCREEN | LAYOUT_HIDE_NAVIGATION | LAYOUT_STABLE
        web.setSystemUiVisibility(0x1000 | View.SYSTEM_UI_FLAG_FULLSCREEN | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION | 0x400 | 0x200 | 0x100);
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) immersive();
    }

    @Override
    protected void onPause() {
        super.onPause();
        web.onPause();
    }

    @Override
    protected void onResume() {
        super.onResume();
        web.onResume();
    }

    @Override
    public void onBackPressed() {
        // the game pauses or steps back a screen; on the title screen it closes the app
        web.loadUrl("javascript:(function(){if(!(window.SAM&&SAM.onBack&&SAM.onBack()))Android.exit();})()");
    }

    private class Bridge {
        @JavascriptInterface
        public void exit() {
            runOnUiThread(new Runnable() {
                public void run() { finish(); }
            });
        }
    }
}
