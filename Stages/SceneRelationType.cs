namespace Exanite.SceneManagement.Stages
{
    public enum SceneRelationType
    {
        /// <summary>
        /// A peer scene will be loaded and activated before the current scene activates.
        /// </summary>
        Peer,

        /// <summary>
        /// A parent scene is the same as a peer scene, but DI bindings made in the parent scene will be made available in the current (child) scene.
        /// The child scene's container will have the parent scene's container as a parent container.
        /// </summary>
        Parent,
    }
}
