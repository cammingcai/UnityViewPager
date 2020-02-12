/// Credit BinaryX 
/// Sourced from - http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/page-2#post-1945602
/// Updated by ddreaper - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.

using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions {
   // [RequireComponent (typeof (ScrollRect))]
    //[AddComponentMenu ("UI/Extensions/Horizontal Scroll Snap")]
    public class HorizontalScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler {
        private Transform _screensContainer;

        private int _screens = 1;
        private int _startingScreen = 1;

        private bool _fastSwipeTimer = false;
        private int _fastSwipeCounter = 0;
        private int _fastSwipeTarget = 30;

        private System.Collections.Generic.List<Vector3> _positions;
        private ScrollRect _scroll_rect;
        private Vector3 _lerp_target;
        private bool _lerp;

        private int _containerSize;

        public GameObject Pagination;

    
        public GameObject NextButton;
     
        public GameObject PrevButton;

        public Boolean UseFastSwipe = true;
        public int FastSwipeThreshold = 0;

        private bool _startDrag = true;
        private Vector3 _startPosition = new Vector3 ();
        private int _currentScreen;
        public float _speed=10;

        private Transform contentItemTf;
      //  private Transform contentTf;
        private RectTransform viewportRT;
        void Start () {
            _scroll_rect = gameObject.GetComponent<ScrollRect> ();
            //contentTf = transform.Find("Viewport/Content");
            contentItemTf = transform.Find("Viewport/Button");
            viewportRT = transform.Find("Viewport").GetComponent<RectTransform>();
            _screensContainer = _scroll_rect.content;
            DistributePages ();
            ChangeBulletsInfo(0);

            for (int i = 0; i < 10; i++)
            {
                //Instantiate实例化物体后出现scale改变
               // 这样实例化出来的物体不会出现scale中的改变（因为没有在外部更改父物体,一次性成品，安全）
                Transform itemT = GameObject.Instantiate(contentItemTf, contentItemTf.localPosition,
                     contentItemTf.localRotation, _screensContainer);
               // itemT.transform.localScale = contentItemTf.localScale;
                //itemT.GetComponent<RectTransform>().sizeDelta = new Vector2(
                //    contentItemTf.GetComponent<RectTransform>().rect.width
                //, contentItemTf.GetComponent<RectTransform>().rect.height);
                Text item = itemT.Find("Text").GetComponent<Text>();
                item.text = ""+i;
               // itemT.SetParent(contentTf);
            }

            _screens = _screensContainer.childCount;
            Debug.Log(_scroll_rect.horizontalNormalizedPosition);

            _lerp = false;

            _positions = new System.Collections.Generic.List<Vector3> ();

            if (_screens > 0) {
                for (int i = 0; i < _screens; ++i) {
                    _scroll_rect.horizontalNormalizedPosition = (float) i / (float) (_screens - 1);
                    Debug.Log(_scroll_rect.horizontalNormalizedPosition);

                    _positions.Add (_screensContainer.localPosition);
                }
            }
          

            _scroll_rect.horizontalNormalizedPosition = (float) (_startingScreen - 1) / (float) (_screens - 1);

            _containerSize = (int) _screensContainer.gameObject.GetComponent<RectTransform> ().offsetMax.x;

            Debug.Log(_scroll_rect.horizontalNormalizedPosition);

            if (NextButton)
                NextButton.GetComponent<Button> ().onClick.AddListener (() => { NextScreen (); });

            if (PrevButton)
                PrevButton.GetComponent<Button> ().onClick.AddListener (() => { PreviousScreen (); });
        }

        void Update () {
            if (_lerp) {
                _screensContainer.localPosition = Vector3.Lerp (_screensContainer.localPosition, _lerp_target, _speed * Time.deltaTime);
                if (Vector3.Distance (_screensContainer.localPosition, _lerp_target) < 0.001f) {
                    _lerp = false;
                }

                //change the info bullets at the bottom of the screen. Just for visual effect
                if (Vector3.Distance (_screensContainer.localPosition, _lerp_target) < 10f) {
                    ChangeBulletsInfo (CurrentScreen ());
                }
            }

            if (_fastSwipeTimer) {
                _fastSwipeCounter++;
            }

        }

        private bool fastSwipe = false; 

        //下一页
        public void NextScreen () {
            if (CurrentScreen () < _screens - 1) {
                
                _lerp = true;
                _lerp_target = _positions[CurrentScreen () + 1];

                ChangeBulletsInfo (CurrentScreen () + 1);
            }
        }

        //上一页
        public void PreviousScreen () {
            if (CurrentScreen () > 0) {
                Debug.Log(CurrentScreen());
                _lerp = true;
                _lerp_target = _positions[CurrentScreen () - 1];

                ChangeBulletsInfo (CurrentScreen () - 1);
            }
        }

        private void NextScreenCommand () {
            if (_currentScreen < _screens - 1) {
                _lerp = true;
                _lerp_target = _positions[_currentScreen + 1];

                ChangeBulletsInfo (_currentScreen + 1);
            }
        }
        private void PrevScreenCommand () {
            if (_currentScreen > 0) {
                _lerp = true;
                _lerp_target = _positions[_currentScreen - 1];

                ChangeBulletsInfo (_currentScreen - 1);
            }
        }

        //获取回到指定位置的坐标
        private Vector3 FindClosestFrom (Vector3 start, System.Collections.Generic.List<Vector3> positions) {
            Vector3 closest = Vector3.zero;
            float distance = Mathf.Infinity;

            foreach (Vector3 position in _positions) {
                if (Vector3.Distance (start, position) < distance) {
                    distance = Vector3.Distance (start, position);
                    closest = position;
                }
            }

            return closest;
        }

        //返回当前屏幕的标识索引
        public int CurrentScreen () {
            //此处需要将_screenContainer的Anchor设置Min(0,0)、Max(1,1);
            float absPoz = Math.Abs (_screensContainer.gameObject.GetComponent<RectTransform> ().offsetMin.x);

            absPoz = Mathf.Clamp (absPoz, 1, _containerSize - 1);

            float calc = (absPoz / _containerSize) * _screens;

            return (int) calc;
        }
        //改变底部标识
        private void ChangeBulletsInfo (int currentScreen) {
            if (Pagination)
                for (int i = 0; i < Pagination.transform.childCount; i++) {
                    Pagination.transform.GetChild (i).GetComponent<Toggle> ().isOn = (currentScreen == i) ?
                        true :
                        false;
                }
        }

        //基于品目分辨率改变——screensContainer子物体的坐标和位置
        private void DistributePages () {
            int _offset = 0;
            int _step = Screen.width;
            int _dimension = 0;

            int currentXPosition = 0;

            for (int i = 0; i < _screensContainer.transform.childCount; i++) {
                RectTransform child = _screensContainer.transform.GetChild (i).gameObject.GetComponent<RectTransform> ();
                currentXPosition = _offset + i * _step;
                child.anchoredPosition = new Vector2 (currentXPosition, 0f);
                child.sizeDelta = new Vector2 (gameObject.GetComponent<RectTransform> ().sizeDelta.x, gameObject.GetComponent<RectTransform> ().sizeDelta.y);
            }

            _dimension = currentXPosition + _offset * -1;

            _screensContainer.GetComponent<RectTransform> ().offsetMax = new Vector2 (_dimension, 0f);
        }

        #region Interfaces
        public void OnBeginDrag (PointerEventData eventData) {
            _startPosition = _screensContainer.localPosition;
            _fastSwipeCounter = 0;
            _fastSwipeTimer = true;
            _currentScreen = CurrentScreen ();
        }

        public void OnEndDrag (PointerEventData eventData) {
            _startDrag = true;
            if (_scroll_rect.horizontal) {
                if (UseFastSwipe) {
                    fastSwipe = false;
                    _fastSwipeTimer = false;
                    if (_fastSwipeCounter <= _fastSwipeTarget) {
                        if (Math.Abs (_startPosition.x - _screensContainer.localPosition.x) > FastSwipeThreshold) {
                            fastSwipe = true;
                        }
                    }
                    if (fastSwipe) {
                        if (_startPosition.x - _screensContainer.localPosition.x > 0) {
                            NextScreenCommand ();
                        } else {
                            PrevScreenCommand ();
                        }
                    } else {
                        _lerp = true;
                        _lerp_target = FindClosestFrom (_screensContainer.localPosition, _positions);
                    }
                } else {
                    _lerp = true;
                    _lerp_target = FindClosestFrom (_screensContainer.localPosition, _positions);
                }
            }
        }

        public void OnDrag (PointerEventData eventData) {
            _lerp = false;
            if (_startDrag) {
                OnBeginDrag (eventData);
                _startDrag = false;
            }
        }
        #endregion
    }
}