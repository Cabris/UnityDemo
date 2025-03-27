using System.Text;
using TMPro;
using UnityEngine;
namespace UnityDemo
{
    public class SimpleLogger : Singleton<SimpleLogger>
    {
        [SerializeField]
        TMP_Text _debugText;
        private StringBuilder _debugStrB = new StringBuilder(5000);
        private bool _isDebugStrDirty = false;

        public static void Log(string msg)
        {
            Instance.DebugStrAppendLine(msg);
        }

        public void DebugStrAppendLine(string msg)
        {
            //Debug.Log($"_debugStrB.Length: {_debugStrB.Length}, msg.Length: {msg.Length}, _debugStrB.Capacity: {_debugStrB.Capacity}");
            if (_debugStrB.Length + msg.Length >= _debugStrB.Capacity)
            {
                _debugStrB.Remove(0, msg.Length);
            }
            _debugStrB.AppendLine(msg);
            _isDebugStrDirty = true;
            Debug.Log(msg);
        }

        private void Update()
        {
            if (_isDebugStrDirty)
            {
                if (_debugText != null)
                    _debugText.text = _debugStrB.ToString();
                _isDebugStrDirty = false;
            }
        }
    }
}