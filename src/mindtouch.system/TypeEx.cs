using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MindTouch {
    public static class TypeEx {
        public static IEnumerable<Type> DiscoverImplementors(this Type @interface) {
            foreach(var type in Assembly.GetExecutingAssembly().GetTypes()) {
                if(!@interface.IsAssignableFrom(type) || !type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition) {
                    continue;
                }
                yield return type;
            }
        }

        public static IEnumerable<object> Instantiate(this IEnumerable<Type> types) {
            foreach(var type in types) {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if(ctor == null) {
                    continue;
                }
                yield return ctor.Invoke(null);
            }
        }
    }
}
