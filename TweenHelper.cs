namespace LightTween
{
	public class TweenHelper
	{

		public static float EaseInOutQuad (float t, float b, float c, float d)
		{
			t /= d / 2;
			if (t < 1)
				return c / 2 * t * t + b;
			t--;
			return -c / 2 * (t * (t - 2) - 1) + b;
		}

		public static float EaseIn (float t, float b, float c, float d)
		{
			float val = (c * (t /= d) * t + b);
			return val;
		}
		
		public static float EaseOut (float t, float b, float c, float d)
		{
			float val = (-c * (t = t / d) * (t - 2) + b);
			return val;
		}
		
		public static float EaseInOut (float t, float b, float c, float d)
		{
			float val = 0f;
			if ((t /= d / 2) < 1)
				val = (c / 2 * t * t + b);
			else
				val = (-c / 2 * ((--t) * (t - 2) - 1) + b);

			return val;
		}

		public static float Linear (float t, float b, float c, float d)
		{
			return (c * t / d + b);
		}

		public static float EaseOutCubic (float t, float b, float c, float d)
		{
			float val = (c * ((t = t / d - 1) * t * t + 1) + b);

			return val;						
		}

	}
}

