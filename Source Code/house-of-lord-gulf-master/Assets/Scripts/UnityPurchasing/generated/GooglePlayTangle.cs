// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("a6vRCcAhVu5hVRtkv2KZrv16A0XzGE4ZKLu1CIzLpH0Nv9pK2fuGjge1NhUHOjE+HbF/scA6NjY2Mjc07vndmxTD6s7+pi+He8EO3iUcPMVJvBSMHRbC9RZKnHV3e4tr+mSprRiOhMkgwL3J2IiTIYgdlRpAHyb+So+Nwo5cZbj742/H8dRnvrlkF9R3CC05hgqSYzayxSnB5eSmiqs0f0EtqNcxBY3zMNdiST8nNKBBjp2xKvtd2hheKQwQlbt4lUDBhDE4Jq0f2F4/9Bfcc4SfaiErIDv5omJngLU2ODcHtTY9NbU2NjeyD9zG+YTV2/p1l7Dz2NJ2JvZ8JRxjkaYx9BohRlQ013RQd3URHRz/9p0/WDuMVWQkeBeiZ5U6JDU0Njc2");
        private static int[] order = new int[] { 4,9,4,6,13,10,11,13,13,10,13,13,12,13,14 };
        private static int key = 55;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
