using UnityEngine;

namespace Geprek.World
{
    /// <summary>
    /// Satu partikel sprite sederhana: mengembang, melayang, lalu memudar.
    /// Dipakai untuk uap, asap, cipratan minyak, dan percikan.
    /// Sengaja tidak memakai ParticleSystem supaya mudah diatur lewat kode
    /// dan ringan untuk target mobile.
    /// </summary>
    public class Puff : MonoBehaviour
    {
        SpriteRenderer _sr;
        float _life, _age, _spin;
        Vector3 _velocity, _gravity;
        float _scaleFrom, _scaleTo;
        Color _colorFrom, _colorTo;

        public bool Busy { get; private set; }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            gameObject.SetActive(false);
        }

        public void Play(Sprite sprite, Vector3 position, int sortingOrder,
                         Color from, Color to, float scaleFrom, float scaleTo,
                         Vector3 velocity, Vector3 gravity, float life, float spin = 0f)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();

            transform.position = position;
            transform.localScale = Vector3.one * scaleFrom;
            transform.localRotation = Quaternion.identity;

            _sr.sprite = sprite;
            _sr.color = from;
            _sr.sortingOrder = sortingOrder;

            _colorFrom = from; _colorTo = to;
            _scaleFrom = scaleFrom; _scaleTo = scaleTo;
            _velocity = velocity; _gravity = gravity;
            _life = Mathf.Max(0.05f, life); _age = 0f; _spin = spin;

            Busy = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;

            if (t >= 1f)
            {
                Busy = false;
                gameObject.SetActive(false);
                return;
            }

            _velocity += _gravity * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(_scaleFrom, _scaleTo, t);
            if (_spin != 0f) transform.Rotate(0f, 0f, _spin * Time.deltaTime);

            if (_sr != null) _sr.color = Color.Lerp(_colorFrom, _colorTo, t);
        }
    }
}
