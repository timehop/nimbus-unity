#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Nimbus.Editor {
	public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject {
		
		private static string repoString = @"
allprojects {
	repositories {
		maven {
			url = uri(""https://adsbynimbus-public.s3.amazonaws.com/android/sdks"")
			credentials {
				username = ""*""
			}
			content {
                includeGroup(""com.adsbynimbus.android"")
                includeGroup(""com.adsbynimbus.openrtb"")
                includeGroup(""com.iab.omid.library.adsbynimbus"")
            }
		}
	}
}";
		private static string dependencies = @"dependencies {
	implementation ""com.adsbynimbus.android:nimbus:1.11.5""
	implementation ""com.adsbynimbus.android:extension-okhttp:1.11.5""
	implementation ""com.adsbynimbus.android:extension-exoplayer:1.11.5""
	implementation ""com.adsbynimbus.openrtb:kotlin-android:0.9.0""
	implementation ""androidx.core:core:1.5.0""
}";

		private static string keepRules = @"
-keep class com.nimbus.demo.UnityHelper { *; }
-keep class com.adsbynimbus.** { *; }";

		private static string packagingOptions = @"
android {
	packagingOptions {
    	pickFirst ""META-INF/annotation-experimental_release.kotlin_module""
		pickFirst ""META-INF/kotlin-stdlib.kotlin_module""
	}
}";

		public int callbackOrder {
			get { return 999; }
		}

		public void OnPostGenerateGradleAndroidProject(string path) {		
			writeGradleProps(path + "/../gradle.properties");
			StreamWriter repoWriter = File.AppendText(path + "/../build.gradle");
			repoWriter.WriteLine(repoString);
			repoWriter.Flush();
			repoWriter.Close();

			StreamWriter buildWriter = File.AppendText(path + "/build.gradle");	
			buildWriter.WriteLine(dependencies);
			buildWriter.Flush();
			buildWriter.Close();

			StreamWriter proguardWriter = File.AppendText(path + "/proguard-unity.txt");
			proguardWriter.WriteLine(keepRules);
			proguardWriter.Flush();
			proguardWriter.Close();

			StreamWriter packagingWriter = File.AppendText(path + "/../launcher/build.gradle");
			packagingWriter.WriteLine(packagingOptions);
			packagingWriter.Flush();
			packagingWriter.Close();	
		}

		private void writeGradleProps(string gradleFile) {
			StreamWriter propWriter = File.AppendText(gradleFile);
			propWriter.WriteLine(@"
android.useAndroidX=true
");
			propWriter.Flush();
			propWriter.Close();
		}
	}
}
#endif