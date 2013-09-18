using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Collections.Generic;

namespace LightTween
{

    public delegate void AnimationComplete();
    public delegate void AnimationUpdate(int value);
    public delegate float TweeningFunction(float timeElapsed, float start, float change, float duration);

    public class Tweener
    {
        List<TweenData> r = new List<TweenData>();

        static readonly Tweener _instance = new Tweener();
        public static Tweener instance { get { return _instance; } }
        private object mutex = new object();
        bool _animating = false;

        private static Dictionary<object, TweenData> _tweenQueue = new Dictionary<object, TweenData>();
        private static Dictionary<object, TweenData> _tweens = new Dictionary<object, TweenData>();

        public Tweener()
        {
            _animating = true;
        }

        public void addTween(TweenData tween)
        {
            tween.start();

            if (!_tweenQueue.ContainsKey(tween.Object))
                _tweenQueue.Add(tween.Object, tween);
            else
                _tweenQueue[tween.Object] = tween;

            _animating = true;
        }

        public void addTween(object o, string property_name, float startVal, float endVal, int duration, TransitionType transitionType, AnimationComplete _completeHandler = null, AnimationUpdate _updateHandler = null)
        {
            TweenData tween = new TweenData(o, property_name, startVal, endVal, duration, transitionType, _completeHandler, _updateHandler);
            this.addTween(tween);
        }

        public void addTween(object o, string property_name, float startVal, float endVal, int duration, TransitionType transitionType = null)
        {
            if (transitionType == null)
                transitionType = TransitionType.LINEAR;

            this.addTween(o, property_name, startVal, endVal, duration, transitionType, null, null);
        }

        public void removeTween(object o, string property_name)
        {
            string name = Tweener.GetName(o, property_name);

            if (_tweenQueue.ContainsKey(o))
                _tweenQueue.Remove(o);
        }

        public void update(GameTime time)
        {
            if (_animating)
            {

                float value;
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                foreach (TweenData tween in _tweens.Values)
                {

                    if (tween == null)
                    {
                        //error("tween value null!!");
                        continue;
                    }

                    if (tween.startTime > now)
                        continue;

                    if (tween.startTime + tween.duration < now)
                    {

                        r.Add(tween);
                        value = tween.endVal;
                        tween.setProperty(value);
                    }
                    else
                    {
                        int elapsed = (int)(now - tween.startTime);
                        value = tween.tweenFunc(elapsed, tween.startVal, tween.endVal - tween.startVal, tween.duration);
                        tween.setProperty(value);
                        tween.updateHandler();
                    }
                }

                clean();
                updateQueue();

                if (_tweens.Count == 0)
                    _animating = false;
            }
        }

        private void clean()
        {
            foreach (TweenData t in r)
            {
                t.completeHandler();
                _tweens.Remove(t.Object);
            }

            r = new List<TweenData>();
        }

        protected void updateQueue()
        {
            lock (mutex)
            {
                foreach (object obj in _tweenQueue.Keys)
                {
                    if (_tweens.ContainsKey(obj))
                    {
                        _tweens[obj] = _tweenQueue[obj];
                    }
                    else
                    {
                        _tweens.Add(obj, _tweenQueue[obj]);
                    }
                }
            }

            _tweenQueue.Clear();
        }


        public static string GetName(object obj, string property)
        {
            string name = "";
            PropertyInfo _prop;
            _prop = obj.GetType().GetProperty("name");
            if (_prop != null)
            {
                name = _prop.GetValue(obj, null) + "_" + property;
                return name;
            }
            else
            {
                name = obj.GetType().FullName + "_" + property;
            }

            return name;
        }

        public static TweeningFunction GetFunction(TransitionType type)
        {
            TweeningFunction tweenFunc;

            switch (type.ToString())
            {
                case "easeIn":
                    tweenFunc = TweenHelper.EaseIn;
                    break;
                case "easeOut":
                    tweenFunc = TweenHelper.EaseOut;
                    break;
                case "easeInOut":
                    tweenFunc = TweenHelper.EaseInOut;
                    break;
                case "linear":
                    tweenFunc = TweenHelper.Linear;
                    break;
                case "easeOutCubic":
                    tweenFunc = TweenHelper.EaseOutCubic;
                    break;
                case "easeInOutQuad":
                default:
                    tweenFunc = TweenHelper.EaseInOutQuad;
                    break;
            }
            return tweenFunc;
        }

    }

    public class TweenData
    {
        protected object _object;
        protected float _startVal = -1;
        protected float _endVal;
        protected long _startTime;


        protected float _val;
        protected long _duration;
        protected long _delayTime;

        protected PropertyInfo _prop;


        protected TweeningFunction _tweenFunc;

        public event AnimationComplete Complete;
        public event AnimationUpdate Update;
        protected string _property_name;
        protected string _name = "";

        #region getters/setters
        public long startTime { get { return _startTime; } set { _startTime = value; } }
        public long delayTime { get { return _delayTime; } set { _delayTime = value; } }
        public string name
        {
            get
            {
                if (_name == "")
                    _name = Tweener.GetName(_object, _property_name);

                return _name;
            }
        }

        public string property_name
        {
            get { return _property_name; }
            set
            {
                _property_name = value;
                updatePropertyInfo();
            }
        }
        public object Object
        {
            get { return _object; }
            set
            {
                _object = value;
                updatePropertyInfo();
            }
        }
        public float startVal { get { return _startVal; } set { _startVal = value; } }
        public float endVal { get { return _endVal; } set { _endVal = value; } }
        public long duration { get { return _duration; } set { _duration = value; } }
        public TweeningFunction tweenFunc { get { return _tweenFunc; } set { _tweenFunc = value; } }
        #endregion

        public TweenData(AnimationComplete completeHandler = null, AnimationUpdate updateHandler = null)
        {
            if (completeHandler != null)
                Complete += completeHandler;

            if (updateHandler != null)
                Update += updateHandler;
        }

        public TweenData(object o, string property_name, float startVal, float endVal, int duration, TransitionType transitionType, AnimationComplete _completeHandler, AnimationUpdate _updateHandler)
            : this(o, property_name, startVal, endVal, duration, transitionType)
        {

            Complete += _completeHandler;
            Update += _updateHandler;
        }

        public TweenData(object o, string property_name, float startVal, float endVal, int duration, TransitionType transitionType)
        {
            this._object = o;
            this._prop = _object.GetType().GetProperty(property_name);
            _startVal = startVal;
            _endVal = endVal == -1 ? float.Parse(_prop.GetValue(_object, null).ToString()) : endVal;
            _duration = duration;
            _property_name = property_name;
            _tweenFunc = Tweener.GetFunction(transitionType);

        }


        private void updatePropertyInfo()
        {
            if (_object != null && _property_name != null)
                this._prop = _object.GetType().GetProperty(property_name);
        }

        public virtual void setProperty(float v)
        {
            _val = v;

            if (_prop.PropertyType == typeof(float))
                _prop.SetValue(_object, v, null);
            else if (_prop.PropertyType == typeof(int))
                _prop.SetValue(_object, (int)v, null);
        }

        public void completeHandler()
        {
            if (Complete != null)
                Complete();
        }

        public void updateHandler()
        {
            if (Update != null)
                Update((int)_val);
        }

        public void start()
        {
            _startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _startTime += _delayTime;
            if (_tweenFunc == null)
                _tweenFunc = Tweener.GetFunction(TransitionType.LINEAR);

            if (_startVal == -1)
                _startVal = float.Parse(_prop.GetValue(_object, null).ToString());
        }
    }

    public class ColorTweenData : TweenData
    {
        protected Color _startColor;
        protected Color _endColor;
        protected Color _currentColor;

        public ColorTweenData(object o, Color startColor, Color endColor, int duration, AnimationComplete completeHandler, AnimationUpdate updateHandler)
        {
            this._object = o;
            this._prop = _object.GetType().GetProperty("color");
            _property_name = "color";
            _startColor = startColor;
            _endColor = endColor;
            _startVal = 0;
            _endVal = 1;
            _duration = duration;
            _tweenFunc = Tweener.GetFunction(TransitionType.LINEAR);
            Complete += completeHandler;
            Update += updateHandler;
        }

        public override void setProperty(float v)
        {
            _val = v;

            float r = _startColor.R + (_endColor.R - _startColor.R) * v;
            float g = _startColor.G + (_endColor.G - _startColor.G) * v;
            float b = _startColor.B + (_endColor.B - _startColor.B) * v;

            _currentColor = new Color((int)r, (int)g, (int)b);
            _prop.SetValue(_object, _currentColor, null);

        }
    }

    public class TransitionType
    {
        public static readonly TransitionType EASE_IN = new TransitionType("easeIn");
        public static readonly TransitionType EASE_OUT = new TransitionType("easeOut");
        public static readonly TransitionType EASE_IN_OUT = new TransitionType("easeInOut");
        public static readonly TransitionType EASE_IN_OUT_QUAD = new TransitionType("easeInOutQuad");
        public static readonly TransitionType EASE_OUT_CUBIC = new TransitionType("easeOutCubic");
        public static readonly TransitionType LINEAR = new TransitionType("linear");

        protected string _name;

        protected TransitionType(string name)
        {
            _name = name;
        }

        public string ToString()
        {
            return _name;
        }
    }
}

