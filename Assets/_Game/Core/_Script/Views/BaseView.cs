using System;
using UnityEngine;
using VContainer.Unity;

namespace ColorBlockJam.Views
{
    public class BaseView: MonoBehaviour, IInitializable
    {
        public virtual void Initialize()
        {
            
        }

        public void DestroyView(float delay = 0f)
        {
            Dispose();
            Destroy(gameObject, delay);
        }

        public virtual void Dispose()
        {
            
        }
    }
}