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


