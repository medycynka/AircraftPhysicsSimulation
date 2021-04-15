﻿using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using MapMagic.Products;

namespace MapMagic.Nodes
{
	[GeneratorMenu (menu = "Objects/Portals", name = "Enter", iconName = "GeneratorIcons/PortalIn")] public class ObjectsPortalEnter : PortalEnter<TransitionsList> { }
	[GeneratorMenu (menu = "Objects/Portals", name ="Exit", iconName = "GeneratorIcons/PortalOut")] public class ObjectsPortalExit : PortalExit<TransitionsList> { }
}