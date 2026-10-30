using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorBlockJam.Views
{
    // Horizontal ScrollRect that settles on a whole page after a drag; a fast flick moves one page.
    [RequireComponent(typeof(ScrollRect))]
    public class MainScreenPageSnapperView : BaseView, IBeginDragHandler, IEndDragHandler
    {
        private const float SnapEpsilon = 0.001f;

        [SerializeField] private float lerpSpeed = 10f;
        [SerializeField] private float minSpeed = 0.1f;
        [SerializeField] private float nextPageVelocityThreshold = 240f;

        private ScrollRect _scrollRect;
        private int _pageCount;
        private int _currentPage;
        private float _targetPosition;
        private bool _snapping;

        public event Action<int, int> PageChanged;

        public int CurrentPage => _currentPage;

        public void Init(int pageCount, int startPage)
        {
            _scrollRect = GetComponent<ScrollRect>();
            _pageCount = Mathf.Max(pageCount, 1);
            _currentPage = ClampPage(startPage);
            SnapImmediately(_currentPage);
        }

        private void Update()
        {
            if (!_snapping) return;

            var current = _scrollRect.horizontalNormalizedPosition;
            var dt = Time.deltaTime;
            var step = Mathf.Max(Mathf.Abs(current - _targetPosition) * lerpSpeed * dt, minSpeed * dt);
            _scrollRect.horizontalNormalizedPosition = Mathf.MoveTowards(current, _targetPosition, step);

            if (Mathf.Abs(_scrollRect.horizontalNormalizedPosition - _targetPosition) < SnapEpsilon)
            {
                _scrollRect.horizontalNormalizedPosition = _targetPosition;
                _snapping = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _snapping = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SnapTo(DetectTargetPage());
        }

        public void SnapTo(int page)
        {
            page = ClampPage(page);
            _targetPosition = PageToPosition(page);
            _scrollRect.velocity = Vector2.zero;
            _snapping = true;
            SetCurrentPage(page);
        }

        public void SnapImmediately(int page)
        {
            page = ClampPage(page);
            _targetPosition = PageToPosition(page);
            _scrollRect.horizontalNormalizedPosition = _targetPosition;
            _snapping = false;
            SetCurrentPage(page);
        }

        private int DetectTargetPage()
        {
            var page = Mathf.RoundToInt(_scrollRect.horizontalNormalizedPosition * (_pageCount - 1));
            if (page == _currentPage)
            {
                // Dragging left gives negative velocity and means "next page".
                var velocity = -_scrollRect.velocity.x;
                if (Mathf.Abs(velocity) > nextPageVelocityThreshold) page += (int)Mathf.Sign(velocity);
            }
            return ClampPage(page);
        }

        private void SetCurrentPage(int page)
        {
            if (page == _currentPage) return;
            var previous = _currentPage;
            _currentPage = page;
            PageChanged?.Invoke(previous, page);
        }

        private float PageToPosition(int page) => _pageCount > 1 ? page / (float)(_pageCount - 1) : 0f;

        private int ClampPage(int page) => Mathf.Clamp(page, 0, _pageCount - 1);
    }
}
