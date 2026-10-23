 ## Phase 1 questions :

1. Why are the sync and async times almost the same when orders are awaited one after another?
async/await مش بيقلل مدة العملية نفسها بالضرورة، لكنه بيمنع حجز الـ Thread أثناء انتظار العمليات غير المتزامنة.
يعني من الاخر هي الافضل بسبب العمليات غير المتزامنه وانه مش بيحجز ال Thread من البلوك 

2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.
اولا اول نقطه هي ان اول ما تستدعي الفانكشن هنخش جواها مع اول await هيقابلنا هيحصل انه الثريد هيطلع عن نقطه الاستدعاء
لو لقينا await عليها يبقى الثريد ده بيحصله انه بيروح اrun time ومش بيعمل اي حجه تانيه وبييجي ثريد من ال ثريدبول يكمل ما تبقى من الفانكشن 
وايضا ممكن هو اللي يكمل البرنامج وممكن ثردي اخر .

3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?
Task.Delay(): دي مش بتعمل بلوك للثريد لاكن الثريد بيتحرر
Thread.Sleeep(): ده بيعمل بلوك للثريد لحد ما الوقت تخلص 

4. Why should a repository method return Task<T> instead of T?
لان ال T فقط بتجبرك انك ترجع في الوقت زاته هزه القيمه اما ال Task<T> هي بتوعدك انك هترجعلك القيمه دي وده احسن لان في عمليات ال i/o 
والكلام اللي هو انت بتجيب بيانات من خارج البرنامج 

5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?
الفرق هنا برضو ان ال async هي مش بتعمل بلوك للثريد مش شرط انها تبقى في وقت اقل لاكن اللي من غير async هي بتعمل بلوك للثريد .


## Phase 2 questions :

1. What is the difference between starting a task and awaiting it?
- بدء الـ Task (Starting a task): معناه إن العملية بتبدأ في الخلفية وبتديلك Task كـ وعد (Promise) بانتهاء الشغل، وفي اللحظة دي الكود بيكمل ومش بيستنى.
- انتظار الـ Task (Awaiting a task): استخدام `await` معناه إنك بتقول للبرنامج استنى لما الـ Task دي تخلص عشان تاخد النتيجة أو تشوف لو فيه Error، ولكن بدون ما تعمل بلوك للـ Thread (الـ Thread بيتحرر يخدم حاجات تانية).

2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?
- هل كل Task بتعمل Thread جديد؟ لأ طبعاً، الـ Task مش Thread، هي بتمثل وحدة عمل غير متزامنة. عمليات الـ I/O مش بتحتاج Thread أصلاً أثناء انتظارها، وعمليات الـ CPU-bound بتستخدم Thread موجود وجاهز في الـ ThreadPool مش بتنشئ Thread جديد من الصفر.
- هل Task.Delay() بتستخدم Thread وهي بتنتظر؟ لأ، بتعتمد على نظام الـ Timers في الـ OS، فالـ Thread بيتحرر بالكامل ومش بيتحجز أثناء مدة الانتظار.

3. What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous? Write a small separate demo method — the only place .Result / .Wait() is allowed.
- بيحصل Block (تجميد) صريح للـ Thread الحالي ويفضل محبوس ومستني لحد ما الـ Task تخلص.
- خطورته:
  1. بيهدر الـ Threads ويعمل Thread Pool Starvation.
  3. بيلف الـ Exceptions جوه AggregateException فبيصعب تتبع الأخطاء.
- المكان الوحيد المقبول: لو بتتعامل مع Main Sync قديم في Console App مش داعم async Main، أو مع legacy synchronous code مضطر تناديه:

public static void LegacySyncMainDemo()
{
    // المكان الوحيد المسموح فيه تاريخياً: كونسول قديم دالته Main متزامنة
    Task<string> task = GetDataAsync();
    string result = task.Result; // أو task.Wait()
    Console.WriteLine(result);
}

private static async Task<string> GetDataAsync()
{
    await Task.Delay(200);
    return "Data Loaded";
}
```

4. What happens to an exception thrown inside a task that is never awaited?
- الـ Exception بيتحفظ جوه كائن الـ Task ومش بيترمي في ساعتها للبرنامج الرئيسي (Silent Failure).
- لما الـ Garbage Collector ييجي يمسح كائن الـ Task بيضرب حدث اسمه `TaskScheduler.UnobservedTaskException`.
- في إصدارات .NET الحديثة مش بيوقع البرنامج بشكل افتراضي، لكن المشكلة إنك مش هتعرف أبداً إن فيه Error حصل لأن الكود كتمه.

5. When should you use Task.Run(), and when should you avoid it?
- استخدمه: مع العمليات الثقيلة على المعالج (CPU-bound work) زي تشفير، معالجة صور، حسابات معقدة عشان تنقلها لـ Background Thread وما تعطلش الـ UI Thread.
- تجنبه: مع عمليات الـ I/O-bound (زي قراءة ملف، داتابيز، API request) لأنها أصلاً غير متزامنة بطبيعتها ومش محتاجة تستهلك Thread من الـ ThreadPool عالفاضي. وتجنبه أيضاً داخل الـ ASP.NET Core controllers.

6. What is the difference between Task and Task<T>?
- الـ `Task`: بتمثل عملية غير متزامنة لكن مش بترجع أي قيمة بعد ما تخلص (زي الـ `void`).
- الـ `Task<T>`: بتمثل عملية غير متزامنة بس بترجع قيمة من النوع `T` بعد ما تخلص (زي مثلاً `Task<int>`, `Task<Order>`).

7. Why can a lock block not contain an await?
- لأن الـ `lock` في C# مبني على الـ Thread نفسه (Thread Affinity)؛ يعني لازم الـ Thread اللي حجز الـ Lock هو نفسه اللي يحرره ويفكه.
- مع الـ `await`، الكود لما يرجع يكمل مش شرط يكمل على نفس الـ Thread، ممكن يكمل على Thread تاني خالص من الـ ThreadPool.
- لو ده حصل جوه lock هيضرب خطأ `SynchronizationLockException`. عشان كده بنستخدم بديل مخصص للـ async وهو `SemaphoreSlim`.

8. Why did the unprotected stock reservation oversell, and how did you prevent it?
- ليه حصل oversell؟ بسبب مشكلة الـ Race Condition، لأن الـ Orders اتنفذت بالتوازي (Parallel)، وكل Order كان بيدخل يقرأ نفس كمية المخزون القديمة في نفس الوقت قبل ما التاني يلحق يخصم، فالاتنين لاقوا كمية كافية وخصموا فحصل بيع بالزيادة.
- منعناها إزاي؟ استخدمنا `SemaphoreSlim(1, 1)` عشان نضمن إن خطوة (قراءة المخزون، والتحقق، والخصم، والحفظ) متقدرش أكتر من Task واحدة تنفذها في نفس اللحظة (Mutual Exclusion)، والـ Tasks التانية تستنى دورها بـ `await _stockSemaphore.WaitAsync(ct)`.


## Phase 3 questions :
1. Does async make one order's workflow faster?
مش شرط هو في الاول والخر الميزه هي عدم عمل bolck لل ثريد عشان لما بقى نخش مثلا في الرامج الكبيره هيظهر الفرق بقى بين ال async , sync 
بس 

2. Why does async scale better for many orders?
اولا عندما البرنامج بتاعك يكبر هتضطر تحول ل async لانها هي مش بتعمل اي بلوك للثريد عندما تكون في عمليه i/o operation لحد ما الناتج ييجي 
لا هي طبعا الثريد بيروح ال thread pool يشوفله شغله تانيه على ما العمليه تخلص وودي هتفيدنا طبعا في البرامج الكبيره

3. What happens to the sync version with 1,000 orders?
طبعا مع ال 1000 اوردر مع كل اوردر هيعمل بلوك لكل حاجه تانيه لحد ما يخلص الاوردر ده ده رقم 1 
رقم 2 عدد الثريدز اللي هتشتغل كبير جدا بسبب انك هنا مش بتستخدم الثريد بول 

4.  Why is async essential for apps that call databases, files and APIs?
مهم جدا اولا بسبب انك مش بتخلي الثريد بتاعك يستنى لحد ما يعمل عمليات ال I/O لا هو بيتحرر لحد ما العمليه دي تخلص وييجي ثريد تاني من الثريد 
بول ممكن يكون هو او واحد تاني وبكدا انت بتكون وفرت wprkers كتير في العمليه بتاعتك 

public async Task ProcessOrderAsync(Order order, Customer customer)
{
    await _orderRepository.SaveAsync(order);
    _emailService.SendConfirmationAsync(order, customer);   // not awaited
    Console.WriteLine("Order processing finished");
}
1. Find the problem in this code.
المشكله هنا انك معملتش await للفانكشن ال async اللي عندك وده غلط

2. Show what happens if the email is still being sent when the method returns, or when the console app exits.
هيحصل انك اولا انك ظهرك رساله ان الايميل اتبعت بس ممكن يون لسه متبعتش وبيتبعت فكدا حصل تاخير ثانيا ممكن البرنامج يقفل ومش 
هيبعت حاجه اصلا وكدا هيبقى فيه حاجه غلط

3. Show what happens if the email operation throws an exception.
هيحصل انك مش هتعرف برضو انه رمى اكسيبشن بسبب انك مش عامل await على الفانكشن فهيحل silent exception مش هيظهر الا لما 
تعمل await عليها

4. async void مش هتنفع تتعمل بسسب ان ال async مش هتنفع تشتغل مع vpid لانها بتشتغل مع task 


## Phase 4 questions :

1. For each of Tasks 10.1–10.4: which tool did you use, and why can't "start, then await one by one" meet the requirement?
- Task 10.1 (Live checkout progress):
  - الأداة: Task.WhenEach (في .NET 9 و .NET 10).
  - ليه start ثم await ورا بعض متنفعش: لأنك لو انتظرتهم بالترتيب (1 ثم 2 ثم 3...)، لو طلب اتأخر 5 ثواني وكان فيه طلب بعده خلص في ثانية واحدة، الشاشة هتفضل معلقة 5 ثواني ومش هتعرض الطلب السريع في وقته، فمش هتحقق متطلب عرض كل طلب فور انتهائه وبترتيب انتهاء العمليات الفعلي.
- Task 10.2 (Customer dashboard):
  - الأداة: Task.WhenAll.
  - ليه start ثم await ورا بعض متنفعش: لأننا محتاجين نشغل الـ 4 استعلامات في نفس اللحظة وننتظرهم معاً. لو عملت await ورا بعض، مع أول خطأ هيحصل الدالة هتقف وتخرج ومش هتقدر تعرف الأخطاء اللي حصلت في باقي العمليات التانية.
- Task 10.3 (Fastest shipping quote):
  - الأداة: Task.WhenAny مع CancellationTokenSource.
  - ليه start ثم await ورا بعض متنفعش: لأن الهدف اختيار أول عرض سعر ناجح وسريع يصل فوراً وإلغاء الباقي، والالتزام بمهلة 1.2 ثانية. الانتظار ورا بعض بيجبرك تستنى الأبطأ أو الفاشل أولاً قبل ما تبص على الباقيين.
- Task 10.4 A (CPU-bound Fraud scoring):
  - الأداة: Parallel.For.
  - ليه start ثم await ورا بعض متنفعش: لأن العمليات هنا حسابية تستهلك المعالج (CPU-bound)، والـ await العادي لا يوزع الحسابات على أنوية الـ CPU بالتساوي. Parallel.For هي المسؤولة عن تقسيم البيانات (Data Partitioning) واستغلال جميع أنوية المعالج (ProcessorCount).
- Task 10.4 B (I/O-bound Email re-send):
  - الأداة: Parallel.ForEachAsync مع MaxDegreeOfParallelism = 4.
  - ليه start ثم await ورا بعض متنفعش: الـ await المتتالي هيخليهم بطيئين جداً واحداً تلو الآخر، وتشغيلهم كلهم مرة واحدة بدون تحكم قد يسبب ضغطاً مفاجئاً ويتجاوز الحد المسموح. Parallel.ForEachAsync توفر خنقاً آمناً للـ Concurrency بحيث لا تزيد المهام قيد التشغيل عن 4 في نفس اللحظة.

2. In 10.2, when several loads fail, how do you get every exception? What does a plain await give you?
- الـ await العادي (Plain await) بيرمي أول Exception حصل فقط ويتجاهل الباقي للتوافق مع try/catch.
- عشان تحصل على كل الأخطاء، بنفحص خاصية task.Exception في كائن الـ Task المجمعة الناتجة من Task.WhenAll، وهي من نوع AggregateException وتضم في خاصية InnerExceptions قائمة بكل الاستثناءات التي حدثت في جميع المهام دون استثناء.

3. In 10.3, what happens to the losing carriers if you never cancel them?
- تظل الشركات الخاسرة مستمرة في العمل في الخلفية (Background execution) تستهلك الشبكة وموارد النظام والـ Sockets بلا أي فائدة، مما يسبب إهداراً للموارد (Resource Leaks) وزيادة استهلاك الـ CPU والترافيك غير الضروري.

4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?
- 10.4 A (Fraud Scoring): عملية CPU-bound تعتمد كلياً على العمليات الحسابية داخل المعالج، لذا كلما زاد عدد الأنوية زادت قدرة المعالج على تنفيذ مهام أكثر في نفس اللحظة الزمنية الحقيقية (True Parallelism).
- 10.4 B (Email Re-send): عملية I/O-bound يضيع أغلب وقتها في انتظار استجابة الشبكة وخادم البريد (Network latency) دون أي مجهود يذكر من المعالج، كما أن معدل التزامن مقيد صراحة برقم ثابت (4 مهام متزامنة كحد أقصى) فلا يؤثر عدد الأنوية في سرعتها.

5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?
- العمل المتوازي (Parallel): مخصص لعمليات الـ CPU-bound، ويحتاج أنوية متعددة حقيقية تعمل معاً على أجزاء مختلفة من البيانات في نفس اللحظة.
- العمل غير المتزامن التزامني (Concurrent Async): مخصص لعمليات الـ I/O-bound، ولا يحتاج أنوية متعددة، بل يعتمد على تحرير الخيوط وعدم عمل Block أثناء انتظار الرد من الشبكة أو الديسك، فيستطيع خيط واحد أو خيوط قليلة إدارة مئات الطلبات المنتظرة.

6. Show a cancelled run (--cancel-after) and explain what happened to the orders, the stock and the payments.
- عند تفعيل الإلغاء عبر CancellationToken (مثلاً بعد 1500ms):
  - الطلبات (Orders): الطلبات التي انتهت قبل المهلة تُسجل Completed، والطلبات التي كانت قيد التنفيذ تتوقف فوراً وترمي OperationCanceledException ولا تكتمل.
  - المخزون (Stock): بفضل استخدام try/finally وكتل معالجة الأخطاء والـ ReleaseStock، أي مخزون كان قد تم حجزه لطلب أُلغي يتم إرجاعه فوراً للمخزن لمنع أي ضياع في المخزون.
  - المدفوعات (Payments): الطلبات المكتملة فقط هي التي تُحفظ مدفوعاتها في payments.json، بينما الطلبات التي أُلغيت قبل إتمام الدفع لا يتم سحب أي أموال لها أو تسجيل أي عملية دفع غير مكتملة.
