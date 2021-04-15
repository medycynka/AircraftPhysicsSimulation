using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using MapMagic.Products;

namespace MapMagic.Nodes
{
	[GeneratorMenu (menu = "Objects/Function", name = "Enter")] public partial class ObjectsFunctionInput : FunctionInput<TransitionsList> { }
	[GeneratorMenu (menu = "Objects/Function", name = "Exit")] public partial class ObjectsFunctionOutput : FunctionOutput<TransitionsList> { }
}