using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : INetworkSceneManager
{
    public bool IsBusy => throw new System.NotImplementedException();

    public Scene MainRunnerScene => throw new System.NotImplementedException();

    public SceneRef GetSceneRef(GameObject gameObject)
    {
        throw new System.NotImplementedException();
    }

    public SceneRef GetSceneRef(string sceneNameOrPath)
    {
        throw new System.NotImplementedException();
    }

    public void Initialize(NetworkRunner runner)
    {
        throw new System.NotImplementedException();
    }

    public bool IsRunnerScene(Scene scene)
    {
        throw new System.NotImplementedException();
    }

    public NetworkSceneAsyncOp LoadScene(SceneRef sceneRef, NetworkLoadSceneParameters parameters)
    {
        throw new System.NotImplementedException();
    }

    public void MakeDontDestroyOnLoad(GameObject obj)
    {
        throw new System.NotImplementedException();
    }

    public bool MoveGameObjectToScene(GameObject gameObject, SceneRef sceneRef)
    {
        throw new System.NotImplementedException();
    }

    public bool OnSceneInfoChanged(NetworkSceneInfo sceneInfo, NetworkSceneInfoChangeSource changeSource)
    {
        throw new System.NotImplementedException();
    }

    public void Shutdown()
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetPhysicsScene2D(out PhysicsScene2D scene2D)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetPhysicsScene3D(out PhysicsScene scene3D)
    {
        throw new System.NotImplementedException();
    }

    public NetworkSceneAsyncOp UnloadScene(SceneRef sceneRef)
    {
        throw new System.NotImplementedException();
    }
}
