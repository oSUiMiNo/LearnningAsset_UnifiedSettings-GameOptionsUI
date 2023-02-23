using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.SettingsGenerator
{
    [CreateAssetMenu(fileName = "ResolutionConnection", menuName = "SettingsGenerator/Connection/ResolutionConnection", order = 4)]
    public class ResolutionConnectionSO : OptionConnectionSO
    {
        /// <summary>
        /// A list of aspect ratios (width, height) to use as a positive filter criteria for the list of resolutions.<br />
        /// If the list is empty then no filtering will occur and all resolutions will be listed.<br />
        /// </summary>
        [Tooltip("A list of aspect ratios(width, height) to use as a positive filter criteria for the list of resolutions.\n" +
                 "If the list is empty then no filtering will occur and all resolutions will be listed.")]
        public List<Vector2Int> AllowedAspectRatios = new List<Vector2Int>();

        /// <summary>
        /// Threshold of how much a resolution can differ from the defined AllowedAspectRatios.<br />
        /// Like if the allowed aspect is 16:9 (w:h), i.e.: 1.77 and this is 0.02f then all ratios between 1.75 and 1.79 are valid. 
        /// </summary>
        [Tooltip("Threshold of how much a resolution can differ from the defined AllowedAspectRatios.\n" +
                 "Like if the allowed aspect is 16:9 (w:h), i.e.: 1.77 and this is 0.02f then all ratios between 1.75 and 1.79 are valid.")]
        public float AllowedAspectRatioDelta = 0.02f;

        protected ResolutionConnection _connection;

        public override IConnectionWithOptions<string> GetConnection()
        {
            if(_connection == null)
                Create();

            return _connection;
        }

        public void Create()
        {
            _connection = new ResolutionConnection();
            _connection.AllowedAspectRatios = AllowedAspectRatios;
            _connection.AllowedAspectRatioDelta = AllowedAspectRatioDelta;
        }

        public override void DestroyConnection()
        {
            if (_connection != null)
                _connection.Destroy();

            _connection = null;
        }
    }
}
