using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// The NetworkPrefabHandler unit tests validates:
    /// Registering with GameObject, NetworkObject, or GlobalObjectIdHash
    /// Newly assigned rotation or position values for newly spawned NetworkObject instances are valid
    /// Destroying a newly spawned NetworkObject instance works
    /// Removing a INetworkPrefabInstanceHandler is removed and can be verified (very last check)
    /// </summary>
    public class NetworkPrefabHandlerTests
    {

        private const string k_TestPrefabObjectName = "NetworkPrefabTestObject";
        private uint m_GlobalObjectIdHashBase = 123456;
        private GameObject MakeValidNetworkPrefab()
        {
            Guid baseObjectID = NetworkManagerHelper.AddGameNetworkObject(k_TestPrefabObjectName + m_GlobalObjectIdHashBase.ToString());
            NetworkObject validPrefab = NetworkManagerHelper.InstantiatedNetworkObjects[baseObjectID];
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(validPrefab, m_GlobalObjectIdHashBase);
            m_GlobalObjectIdHashBase++;
            return validPrefab.gameObject;
        }

        /// <summary>
        /// Tests the NetwokConfig NetworkPrefabs initialization during NetworkManager's Init method to make sure that
        /// it will still initialize but remove the invalid prefabs
        /// </summary>
        [Test]
        public void NetworkConfigInvalidNetworkPrefabTest()
        {

            // Add null entry
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(null);

            // Add a NetworkPrefab with no prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab());

            // Add a NetworkPrefab override with an invalid hash
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 0 });

            // Add a NetworkPrefab override with a valid hash but an invalid target prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 654321, OverridingTargetPrefab = null });

            // Add a NetworkPrefab override with a valid hash to override but an invalid target prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourceHashToOverride = 654321, OverridingTargetPrefab = null });

            // Add a NetworkPrefab override with an invalid source prefab to override
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = null });

            // Add a NetworkPrefab override with a valid source prefab to override but an invalid target prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = MakeValidNetworkPrefab(), OverridingTargetPrefab = null });

            // Add a valid prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = MakeValidNetworkPrefab() });

            // Add a NetworkPrefab override with a valid hash and valid target prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 11111111, OverridingTargetPrefab = MakeValidNetworkPrefab() });

            // Add a NetworkPrefab override with a valid prefab and valid target prefab
            NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = MakeValidNetworkPrefab(), OverridingTargetPrefab = MakeValidNetworkPrefab() });

            var exceptionOccurred = false;
            try
            {
                NetworkManagerHelper.NetworkManagerObject.StartHost();
            }
            catch
            {
                exceptionOccurred = true;
            }

            Assert.False(exceptionOccurred);

            // In the end we should only have 3 valid registered network prefabs
            Assert.True(NetworkManagerHelper.NetworkManagerObject.NetworkConfig.NetworkPrefabOverrideLinks.Count == 3);
        }

        private const string k_PrefabObjectName = "NetworkPrefabHandlerTestObject";

        [Test]
        public void NetworkPrefabHandlerClass()
        {
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out _));
            var testPrefabObjectName = k_PrefabObjectName;

            Guid baseObjectID = NetworkManagerHelper.AddGameNetworkObject(testPrefabObjectName);
            NetworkObject baseObject = NetworkManagerHelper.InstantiatedNetworkObjects[baseObjectID];

            var networkPrefabHandler = new NetworkPrefabHandler();
            var networkPrefaInstanceHandler = new NetworkPrefaInstanceHandler(baseObject);

            var prefabPosition = new Vector3(1.0f, 5.0f, 3.0f);
            var prefabRotation = new Quaternion(1.0f, 0.5f, 0.4f, 0.1f);

            //Register via GameObject
            var gameObjectRegistered = networkPrefabHandler.AddHandler(baseObject.gameObject, networkPrefaInstanceHandler);

            //Test result of registering via GameObject reference
            Assert.True(gameObjectRegistered);

            var spawnedObject = networkPrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            networkPrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            networkPrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            //Register via NetworkObject
            gameObjectRegistered = networkPrefabHandler.AddHandler(baseObject, networkPrefaInstanceHandler);

            //Test result of registering via NetworkObject reference
            Assert.True(gameObjectRegistered);

            //Change it up
            prefabPosition = new Vector3(2.0f, 1.0f, 5.0f);
            prefabRotation = new Quaternion(4.0f, 1.5f, 5.4f, 5.1f);

            spawnedObject = networkPrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            networkPrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            networkPrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            //Register via GlobalObjectIdHash
            gameObjectRegistered = networkPrefabHandler.AddHandler(baseObject.GlobalObjectIdHash, networkPrefaInstanceHandler);

            //Test result of registering via GlobalObjectIdHash reference
            Assert.True(gameObjectRegistered);

            //Change it up
            prefabPosition = new Vector3(6.0f, 4.0f, 1.0f);
            prefabRotation = new Quaternion(3f, 2f, 4f, 1f);

            spawnedObject = networkPrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            networkPrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            networkPrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            Assert.False(networkPrefaInstanceHandler.StillHasInstances());
        }

        [SetUp]
        public void Setup()
        {
            //Create, instantiate, and host
            NetworkManagerHelper.StartNetworkManager(out _, NetworkManagerHelper.NetworkManagerOperatingMode.None);
        }

        [TearDown]
        public void TearDown()
        {
            //Stop, shutdown, and destroy
            NetworkManagerHelper.ShutdownNetworkManager();

            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>().ToList();
            var networkObjectsList = networkObjects.Where(c => c.name.Contains(k_PrefabObjectName));
            foreach (var networkObject in networkObjectsList)
            {
                UnityEngine.Object.DestroyImmediate(networkObject);
            }
        }
    }

    /// <summary>
    /// The Prefab instance handler to use for this test
    /// </summary>
    public class NetworkPrefaInstanceHandler : INetworkPrefabInstanceHandler
    {
        private NetworkObject m_NetworkObject;

        private List<NetworkObject> m_Instances;

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var networkObjectInstance = UnityEngine.Object.Instantiate(m_NetworkObject.gameObject).GetComponent<NetworkObject>();
            networkObjectInstance.transform.position = position;
            networkObjectInstance.transform.rotation = rotation;
            m_Instances.Add(networkObjectInstance);
            return networkObjectInstance;
        }

        public void Destroy(NetworkObject networkObject)
        {
            var instancesContainsNetworkObject = m_Instances.Contains(networkObject);
            Assert.True(instancesContainsNetworkObject);
            m_Instances.Remove(networkObject);
            UnityEngine.Object.Destroy(networkObject.gameObject);
        }

        public bool StillHasInstances()
        {
            return (m_Instances.Count > 0);
        }

        public NetworkPrefaInstanceHandler(NetworkObject networkObject)
        {
            m_NetworkObject = networkObject;
            m_Instances = new List<NetworkObject>();
        }
    }
}