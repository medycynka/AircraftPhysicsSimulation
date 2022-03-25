using System;
using System.Collections.Generic;
using Aerodynamics.CoreScripts;
using Aerodynamics.CoreScripts.EnvironmentUtilities;
using UnityEngine;


namespace Aerodynamics.Utils
{
    public enum RegionType
    {
        Temperature,
        Wind,
        Both
    }
    
    public class RegionsGenerator : MonoBehaviour
    {
        public RegionType regionType = RegionType.Temperature;
        public Vector3 firstRegionPosition;
        public Vector3 nextRegionOffset;
        public Vector3 regionSize;
        public bool validate;
        public List<ScriptableObject> regions;

        [SerializeField] private List<GameObject> _children = new List<GameObject>();
        // [SerializeField] private Transform _self;
        // [SerializeField] private Transform _player;

        private void OnValidate()
        {
            if (validate && _children.Count > 0)
            {
                for (int i = 0; i < _children.Count; i++)
                {
                    _children[i].transform.position = firstRegionPosition + i * nextRegionOffset;
                    _children[i].GetComponent<BoxCollider>().size = regionSize;
                }
            }
        }

        // private void Awake()
        // {
        //     _self = transform;
        //     _player = FindObjectOfType<PhysicsManager>().gameObject.transform;
        // }
        //
        // private void Update()
        // {
        //     if (Time.frameCount % 60 == 0)
        //     {
        //         Vector2 currDist = new Vector2(_self.position.x - _player.position.x, _self.position.z - _player.position.z);
        //
        //         if (currDist.sqrMagnitude > 10000)
        //         {
        //             _self.position = new Vector3(_player.position.x, _self.position.y, _player.position.z);
        //         }
        //     }
        // }

        public void GenerateRegions()
        {
            if (_children.Count > 0)
            {
                ClearRegions();
            }

            Transform parent = transform;

            for (int i = 0; i < regions.Count; i++)
            {
                GameObject newRegion = new GameObject($"Region {i}")
                {
                    transform =
                    {
                        parent = parent,
                        position = firstRegionPosition + i * nextRegionOffset
                    }
                };
                
                BoxCollider regionCollider = newRegion.AddComponent<BoxCollider>();
                regionCollider.size = regionSize;
                regionCollider.isTrigger = true;

                switch (regionType)
                {
                    case RegionType.Temperature:
                        TemperatureRegionManager trm = newRegion.AddComponent<TemperatureRegionManager>();
                        trm.airRegion = (AirRegion)regions[i];
                        break;
                    case RegionType.Wind:
                        break;
                    case RegionType.Both:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _children.Add(newRegion);
            }
        }
        
        public void ClearRegions()
        {
            Debug.Log($"Clearing {_children.Count} regions");
            
            for (int i = 0; i < _children.Count; i++)
            {
#if UNITY_EDITOR
                DestroyImmediate(_children[i]);
#else
                Destroy(_children[i])
#endif
            }
            
            _children.Clear();
        }
    }
}