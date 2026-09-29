using System.Collections.Generic;
using UnityEngine;

namespace NeuronWorld
{
    /// <summary>Short-lived visual effects (lightning, meteor impacts, spawn rings) and the brush outline.</summary>
    public sealed class EffectsRenderer
    {
        const int SortEffects = 30;
        const int SortBrush = 40;

        sealed class Effect
        {
            public SpriteRenderer renderer;
            public float age;
            public float duration;
            public Color color;
            public Vector3 startScale;
            public Vector3 endScale;
        }

        readonly Transform root;
        readonly Material material;
        readonly List<Effect> active = new List<Effect>();
        readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        readonly SpriteRenderer brush;

        public EffectsRenderer(Transform parent, Material material)
        {
            this.material = material;
            root = new GameObject("Effects").transform;
            root.SetParent(parent, false);
            brush = Create();
            brush.name = "Brush";
            brush.sprite = SpriteFactory.Ring;
            brush.sortingOrder = SortBrush;
            brush.gameObject.SetActive(false);
        }

        SpriteRenderer Create()
        {
            var go = new GameObject("Effect");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = SortEffects;
            return sr;
        }

        public void ShowBrush(Vector2 position, float radius, Color color)
        {
            if (!brush.gameObject.activeSelf) brush.gameObject.SetActive(true);
            brush.transform.localPosition = new Vector3(position.x, position.y, 0f);
            float d = radius * 2f;
            brush.transform.localScale = new Vector3(d, d, 1f);
            brush.color = color;
        }

        public void HideBrush()
        {
            if (brush.gameObject.activeSelf) brush.gameObject.SetActive(false);
        }

        /// <summary>An expanding, fading circle.</summary>
        public void Flash(Vector2 position, float radius, Color color, float duration, float growth = 1.6f)
        {
            float d = radius * 2f;
            Spawn(SpriteFactory.Circle, new Vector3(position.x, position.y, 0f), Quaternion.identity,
                new Vector3(d, d, 1f), new Vector3(d * growth, d * growth, 1f), color, duration);
        }

        /// <summary>An expanding ring, e.g. where creatures were spawned.</summary>
        public void Pulse(Vector2 position, float radius, Color color, float duration)
        {
            float d = radius * 2f;
            Spawn(SpriteFactory.Ring, new Vector3(position.x, position.y, 0f), Quaternion.identity,
                new Vector3(d * 0.3f, d * 0.3f, 1f), new Vector3(d, d, 1f), color, duration);
        }

        /// <summary>A lightning bolt striking down onto a point.</summary>
        public void Bolt(Vector2 position)
        {
            const float length = 40f;
            float angle = Random.Range(-12f, 12f);
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 mid = new Vector3(position.x, position.y, 0f) + rot * new Vector3(0f, length * 0.5f, 0f);
            Spawn(SpriteFactory.Square, mid, rot, new Vector3(0.6f, length, 1f), new Vector3(0.1f, length, 1f),
                new Color(1f, 1f, 0.85f, 1f), 0.25f);
            Flash(position, 3f, new Color(1f, 1f, 0.8f, 0.9f), 0.35f, 2.5f);
        }

        void Spawn(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 startScale, Vector3 endScale, Color color, float duration)
        {
            SpriteRenderer sr = pool.Count > 0 ? pool.Pop() : Create();
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.color = color;
            Transform t = sr.transform;
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = startScale;
            active.Add(new Effect
            {
                renderer = sr,
                duration = Mathf.Max(0.01f, duration),
                color = color,
                startScale = startScale,
                endScale = endScale
            });
        }

        public void Update(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Effect e = active[i];
                e.age += deltaTime;
                float t = e.age / e.duration;
                if (t >= 1f)
                {
                    e.renderer.gameObject.SetActive(false);
                    pool.Push(e.renderer);
                    active.RemoveAt(i);
                    continue;
                }
                float ease = 1f - (1f - t) * (1f - t);
                e.renderer.transform.localScale = Vector3.Lerp(e.startScale, e.endScale, ease);
                Color c = e.color;
                c.a *= 1f - t;
                e.renderer.color = c;
            }
        }
    }
}
