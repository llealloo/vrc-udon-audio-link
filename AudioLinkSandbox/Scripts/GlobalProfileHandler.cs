#define AVERAGE_OUTPUT
using UnityEngine;

#if UDON
using UdonSharp;
[DefaultExecutionOrder(1000000000)]
public class GlobalProfileHandler : UdonSharpBehaviour
#else
[DefaultExecutionOrder(1000000000)]
public class GlobalProfileHandler : MonoBehaviour
#endif
{
    public UnityEngine.UI.Text _timeText;
    private GlobalProfileKickoff _kickoff;

    private void Start()
    {
        _kickoff = GetComponent<GlobalProfileKickoff>();
    }

    private int _currentFrame = -1;
    private float _elapsedTime = 0f;
#if AVERAGE_OUTPUT
    private float _measuredTimeTotal = 0f;
    private int _measuredTimeFrameCount = 0;
    private const int MEASURE_FRAME_AMOUNT = 45;
#endif

    private void FixedUpdate()
    {
        if (_currentFrame != Time.frameCount)
        {
            _elapsedTime = 0f;
            _currentFrame = Time.frameCount;
        }

        if (_kickoff)
            _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
    }

    private void Update()
    {
        if (_currentFrame != Time.frameCount) // FixedUpdate didn't run this frame, so reset the time
            _elapsedTime = 0f;

        _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
    }

    private void LateUpdate()
    {
        _elapsedTime += (float)_kickoff.stopwatch.Elapsed.TotalSeconds * 1000f;
#if AVERAGE_OUTPUT
        if (_measuredTimeFrameCount >= MEASURE_FRAME_AMOUNT)
        {
            _timeText.text = $"{(_measuredTimeTotal / _measuredTimeFrameCount):F4}ms";
            _measuredTimeTotal = 0f;
            _measuredTimeFrameCount = 0;
        }
        _measuredTimeTotal += _elapsedTime;
        _measuredTimeFrameCount += 1;
#else
        _timeText.text = $"Update time:\n{_elapsedTime:F4}ms";
#endif
    }
}