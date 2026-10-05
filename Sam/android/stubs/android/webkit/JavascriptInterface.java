package android.webkit;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/** Compile-time stand-in: the stub android.jar predates API 17. Not packaged; the device provides the real one. */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.METHOD)
public @interface JavascriptInterface {}
