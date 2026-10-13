# Assignment 05 — Answers

Student name:  عبدالرحمن عزت فتوح 
Group / ID: #2

## Failures you observed
باختصار ان المشكله هنا اللي بيتسبب فيها هي ال Dictionary, Queue لانهم هم الاتنين مش Thread safe عشان كدا ال Outbut ضرب Exception 
وده كلو بسبب القانكشن الي ب check the add or apply any thing لان ال check دي بتسمح ان اكتر من thread يخشوا مع بعض في نفس الوقت وده طبعا 
خطا  , ايضا عمليه الزياده دي هتهمل lost كتير جدا للزياده 
-

## What you changed
نستبدل كل Dictionary ب concurrent dictionary وكل Queue ب concurrent queue عشان الthread safe ونغير الفاكشنز بقى باللي ينسب ال program
وكمان هشيل الا Try / catch الي في ال method اللي اسمها EmailWorker عشان ال TryDequeue عمرها ما هترمي Exception 
-

## Collection safety vs business-rule safety
ببساطه انك تستخدم ال collection safe زي اي واحد من اللي استخدمناهم مش شرط يكون business-rule safe لانك ممكن تبوظ اصلا الدنيا وبرضو يفضل الكود عنك معالجش حاجه
Check-then-Act بيكسر القاعدة حتى على Collection آمنة ← استخدم TryAdd / GetOrAdd / TryDequeue
لو القاعدة بتشمل أكتر من عملية أو Collection ← lock واحد حواليهم
List<T> مش آمنة، لازم lock

الخلاصه :
الـ Concurrent Collection بتحمي كل عملية لوحدها. لو قاعدتك بتعتمد على أكتر من عملية مع بعض، لازم إنت تحميها (بـ lock، أو بعملية Atomic واحدة زي TryAdd / TryDequeue، أو بترتيب صح للخطوات).

-
