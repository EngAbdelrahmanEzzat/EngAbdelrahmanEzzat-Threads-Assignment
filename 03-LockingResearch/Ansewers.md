# Assignment 03 — Answers

Student name:  عبدالرحمن عزت فتوح 
Group / ID: #2

1. الـ Monitor كلاس في .NET بيعمل حاجتين:
Mutual Exclusion (الحماية): Monitor.Enter / Monitor.Exit بيخلّوا Thread واحد بس يدخل الكود المحمي في نفس الوقت، فمفيش Race Condition.
التنسيق بين Threads: Wait / Pulse / PulseAll بيخلّوا Thread ينام لحد ما Thread تاني يبعتله إشارة (زي Producer/Consumer).
وفيه كمان Monitor.TryEnter اللي بيحاول ياخد القفل من غير ما يستنى للأب

2. الـ lock مجرد اختصار فوق Monitor. الكود ده:

csharp
lock (obj)
{
    // الكود المحمي
}

بيتحوّل تقريبًا لـ:

csharp
bool lockTaken = false;
try
{
    Monitor.Enter(obj, ref lockTaken);
    // الكود المحمي
}
finally
{
    if (lockTaken)
        Monitor.Exit(obj);
}

3. lock: Timeout:مفيش بيستنى للابد , wait/pulse:مش موجودن و حمايه فقط , Exception: Exit تلقاءي لانه الكود بيتحول ل try/catch
   monitor:Timeout:TryEnter(obj,time) بيرجع false لو معرفش ياخد lock, wait/pulse:يوجد pulseAll, pulse, wait , Exception: إنت اللي بتكتب try/finally، ولو نسيت ممكن يحصل Deadlock

4. Monitor.TryEnter: لما مش عايز تستنى القفل للأبد: عايز Timeout، أو الشغل اختياري (لو القفل مشغول اتخطاه)، أو عايز تكتشف Deadlock.
Monitor.Wait: لما Thread لازم يستنى شرط يتحقق (زي Consumer مستني داتا في Queue) من غير Busy-wait. الـ Wait بتسيب القفل وهي نايمة.
Monitor.Pulse / PulseAll: لما تغيّر الشرط وعايز تصحّي المستنيين. Pulse بتصحّي Thread واحد، و PulseAll بتصحّي الكل.

5. static readonly object locker = new object();
static int counter = 0;

 1) باستخدام lock
static void IncrementWithLock()
{
    lock (locker)
    {
        counter++;
    }
}

 2) باستخدام Monitor.Enter / Monitor.Exit
static void IncrementWithMonitor()
{
    bool taken = false;
    try
    {
        Monitor.Enter(locker, ref taken);
        counter++;
    }
    finally
    {
        if (taken)
            Monitor.Exit(locker);
    }
}

Links:Monitor :https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor
      lock: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock

-
