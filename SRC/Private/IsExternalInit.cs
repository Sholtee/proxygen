/********************************************************************************
* IsExternalInit.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

//
// NET5_0 elotti runtime-on a C# 9 init accessor hasznalata csak hack reven megoldott:
// https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
// Ebben az esetben viszont az osszes TFM szamara definialni kell az IsExternalInit osztalyt:
// https://twitter.com/aarnott/status/1362786409954766858
//

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}
