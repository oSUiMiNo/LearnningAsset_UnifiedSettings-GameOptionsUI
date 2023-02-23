using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.SettingsGenerator
{
    public class ResolutionConnection : ConnectionWithOptions<string>
    {
        /// <summary>
        /// It is not advisable to change the resolution on mobile.
        /// It may have unexpected sideeffect and there usually is
        /// just one anyways.
        /// 
        /// </summary>
        public static bool AllowResolutionChangeOnMobile = false;

        /// <summary>
        /// Disable if the resolutions change very often.
        /// </summary>
        public static bool CacheResolutions = true;

        public event System.Action OnMaxResolutionChanged;

        /// <summary>
        /// A list of aspect ratios (width, height) to use as a positive filter criteria for the list of resolutions.<br />
        /// If the list is empty then no filtering will occur and all resolutions will be listed.<br />
        /// </summary>
        public List<Vector2Int> AllowedAspectRatios = new List<Vector2Int>();

        /// <summary>
        /// Threshold of how much a resolution can differ from the defined AllowedAspectRatios.<br />
        /// Like if the allowed aspect is 16:9 (w:h), i.e.: 1.77 and this is 0.02f then all ratios between 1.75 anf 1.79 are valid. 
        /// </summary>
        public float AllowedAspectRatioDelta = 0.02f;

        protected List<Resolution> _values;
        protected List<string> _labels;

        // We use this value to detect whether or not the avilable resolutions have changed.
        // This usually happens if the app has been moved to another monitor.
        protected Vector2Int _lastMonitorMaxResolution;

        protected Vector2Int getCurrentMaxResolution()
        {
            var resolutions = Screen.resolutions;
            return new Vector2Int(resolutions[resolutions.Length - 1].width, resolutions[resolutions.Length - 1].height);
        }

        protected List<Resolution> getUniqueResolutions()
        {
            if (_values.IsNullOrEmpty() || !CacheResolutions)
            {
                _values = new List<Resolution>();

                // Generate a list of resolutions which have the same refresh rate as the current one.
                var resolutions = Screen.resolutions;
                fillResolutionsList(resolutions, limitAspectRatios: true);
                // If no resolutions are found then don't filter.
                if(_values.Count == 0)
                {
                    Logger.LogWarning("Resolution aspect ratio limiting resulted in an empty list. Disabling filtering (all resolutions will be listed).");
                    fillResolutionsList(resolutions, limitAspectRatios: false);
                }

                // Hard fallback
                if (_values.Count == 0)
                {
                    var res = new Resolution();
                    res.width = 1024;
                    res.height = 768;
#if UNITY_2022_2_OR_NEWER
                    var r = new RefreshRate();
                    r.numerator = 60000;
                    r.denominator = 1001;
                    res.refreshRateRatio = r;
#else
                    res.refreshRate = 60;
#endif
                    _values.Add(res);
                }
            }

            return _values;
        }

        private void fillResolutionsList(Resolution[] resolutions, bool limitAspectRatios)
        {
            foreach (var res in resolutions)
            {
#if UNITY_2022_2_OR_NEWER
                    if (Screen.currentResolution.refreshRateRatio.Equals(res.refreshRateRatio))
#else
                // Weirdly sometimes the current refreshrate is 59 but the rate in all resolutions is 60.
                // To avoid empty resolution lists we allow +/-1.
                if (Mathf.Abs(Screen.currentResolution.refreshRate - res.refreshRate) <= 1)
#endif
                {
                    // Filter res by aspect ratios
                    if (limitAspectRatios && AllowedAspectRatios != null && AllowedAspectRatios.Count > 0)
                    {
                        float ratio = (float)res.width / res.height;
                        foreach (var aspect in AllowedAspectRatios)
                        {
                            float allowedRatio = (float)aspect.x / aspect.y;
                            if (Mathf.Abs(ratio - allowedRatio) <= AllowedAspectRatioDelta)
                            {
                                _values.Add(res);
                            }
                        }
                    }
                    else
                    {
                        // No filtering
                        _values.Add(res);
                    }
                }
            }
        }

        public override List<string> GetOptionLabels()
        {
            // Reset values and labels if monitor max resolution changed.
            var maxResolution = getCurrentMaxResolution();
            if (maxResolution != _lastMonitorMaxResolution)
            {
                _lastMonitorMaxResolution = maxResolution;

                _values = null;
                _labels = null;

                OnMaxResolutionChanged?.Invoke();
            }

            if (_labels.IsNullOrEmpty() || !CacheResolutions)
            {
                _labels = new List<string>();

                var resolutions = getUniqueResolutions();
                foreach (var res in resolutions)
                {
                    string name = res.width + "x" + res.height;
                    _labels.Add(name);
                }
            }

            return _labels;
        }

        public override void RefreshOptionLabels()
        {
            _labels = null;
            GetOptionLabels();
        }

        public override void SetOptionLabels(List<string> optionLabels)
        {
            var resolutions = getUniqueResolutions();
            if (optionLabels == null || optionLabels.Count != resolutions.Count)
            {
                Logger.LogError("Invalid new labels. Need to be " + resolutions.Count + ".");
            }

            _labels = new List<string>(optionLabels);
        }

        protected Resolution? lastKnownResolution = null;
        protected int lastSetFrame = 0;

        public override int Get()
        {
            // Reset after N frames. The assumption is that
            // after that the Screen.currentResolution has been updated.
            if (Time.frameCount - lastSetFrame > 3)
                lastKnownResolution = null;

            Resolution currentResolution = Screen.currentResolution;
            if (lastKnownResolution.HasValue)
                currentResolution = lastKnownResolution.Value;

            // Find the closest resolution. Usually they match exactly but after
            // a monitor changed they may not so we search for the best match.
            var resolutions = getUniqueResolutions();
            int minDelta = int.MaxValue;
            int closestResolutionIndex = 0;
            for (int i = 0; i < resolutions.Count; i++)
            {
                int delta = Mathf.Abs(resolutions[i].width - currentResolution.width) + Mathf.Abs(resolutions[i].height - currentResolution.height);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    closestResolutionIndex = i;

                    // Shortcut
                    if (minDelta == 0)
                        return i;
                }
            }

            return closestResolutionIndex;
        }

        /// <summary>
        /// NOTICE: This has no effect in the Edtior.<br />
        /// NOTICE: A resolution switch does not happen immediately; it happens when the current frame is finished.<br />
        /// See: https://docs.unity3d.com/ScriptReference/Screen.SetResolution.html
        /// </summary>
        /// <param name="index"></param>
        public override void Set(int index)
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
            if (!AllowResolutionChangeOnMobile)
            {
                Debug.LogWarning("Allow resolution change on mobile is disabled. It is not advisable to change the resolution on mobile. It may have unexpected sideeffects and there usually is just one anyways. If you are on URP then use the renderScale instead.");
                return;
            }
#endif

            var resolutions = getUniqueResolutions();
            index = Mathf.Clamp(index, 0, Mathf.Max(0, resolutions.Count - 1));
            var resolution = resolutions[index];

            // Request change but delegate the actual execution to the orchestrator.
            ScreenOrchestrator.Instance.RequestResolution(resolution);

            // remember
            lastSetFrame = Time.frameCount;
            lastKnownResolution = resolution;

            NotifyListenersIfChanged(index);

#if UNITY_EDITOR
            if (SettingsGeneratorSettings.GetOrCreateSettings().ShowEditorInfoLogs)
            {
                Logger.LogMessage("Setting the resolution has no effect in the Editor. Please try in a build. - " + SettingsGeneratorSettings._showEditorInfoLogsHint);
            }
#endif
        }
    }
}
