namespace LvStreamStore {
    internal static class StreamExtensions {
        public static byte[] ToArray(this Stream source) {
            if (source is MemoryStream stream) {
                return stream.ToArray();
            }

            using (var ms = new MemoryStream()) {
                source.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
