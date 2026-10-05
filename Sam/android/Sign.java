import com.android.apksig.ApkSigner;
import com.android.apksig.ApkVerifier;
import java.io.File;
import java.io.FileInputStream;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.cert.X509Certificate;
import java.util.Collections;

/**
 * Signs an APK with the v2 scheme using apksig, then verifies it. The app needs Android 7.0+, which reads v2;
 * the old JAR (v1) signer in apksig 2.3.0 does not run on current JDKs.
 * Usage: Sign keystore.p12 password in.apk out.apk
 */
public class Sign {
    public static void main(String[] a) throws Exception {
        KeyStore ks = KeyStore.getInstance("PKCS12");
        try (FileInputStream in = new FileInputStream(a[0])) { ks.load(in, a[1].toCharArray()); }
        String alias = ks.aliases().nextElement();
        PrivateKey key = (PrivateKey) ks.getKey(alias, a[1].toCharArray());
        X509Certificate cert = (X509Certificate) ks.getCertificate(alias);
        ApkSigner.SignerConfig signer = new ApkSigner.SignerConfig.Builder("sam", key, Collections.singletonList(cert)).build();
        new ApkSigner.Builder(Collections.singletonList(signer))
                .setInputApk(new File(a[2])).setOutputApk(new File(a[3]))
                .setV1SigningEnabled(false).setV2SigningEnabled(true)
                .build().sign();
        ApkVerifier.Result r = new ApkVerifier.Builder(new File(a[3])).build().verify();
        System.out.println("signature verified: " + r.isVerified() + " (v1 " + r.isVerifiedUsingV1Scheme() + ", v2 " + r.isVerifiedUsingV2Scheme() + ")");
        for (Object e : r.getErrors()) System.out.println("error: " + e);
        for (Object w : r.getWarnings()) System.out.println("warning: " + w);
        if (!r.isVerified()) System.exit(1);
    }
}
