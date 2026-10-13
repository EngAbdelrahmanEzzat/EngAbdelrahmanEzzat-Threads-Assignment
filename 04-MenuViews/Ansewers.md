# Assignment 04 — Answers

Student name:  
Group / ID:

## Why Expected and Actual disagree
Expected: 400000
Actual:   104593 
ليه حصل كدا لان اولا العمليه مش thread safe بسبب ال Race condition لان عمليه الزياده دي هي في الحقيقه هي 3 عمليات وهي وبكدا ممكن يحصل lost للزياده 

-

## What you changed
لان دي عمليه واحده للزياده بدل ما اعمل lock عشان عمليه واحده احنا عندنا حاجه اسمها interlocked وهي بتخلي العمليه بتاعه الزياده او النقصان 
atomic operation وبكدا بقت thread safe 
-

## When would you protect the update differently
ممكن نحميه برضو ب lock عادي جدا ان احنا ننشي اوبجكت وبعدين تاخد لوك عليه 
او برضو كلاس ال monitor / monitor.Enter, monitor.Exit
.-




## After Solution 
Expected: 400000
Actual:   400000 