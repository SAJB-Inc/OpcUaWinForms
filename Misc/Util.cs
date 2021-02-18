using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpcUaWinForms {
    static class Util {
        public static Icon ReadEmbeddedIcon(string name) {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));
            using (var stream = assembly.GetManifestResourceStream(resourcePath))
                return new Icon(stream);
        }
    }
}
