# Assignment 01 — Answers

Student name:  عبدالرحمن عزت فتوح
Group / ID: #2
## ORIGINAL OUTPUT(BEFORE CHANGES)
Starting invoice export...
Customer files: E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\InvoiceExport\bin\Debug\net9.0\Data
Invoice output: E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices

Wrote E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices\invoice-1001.txt
Wrote E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices\invoice-1002.txt
Wrote E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices\invoice-1003.txt
Wrote E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices\invoice-1004.txt
Wrote E:\AdvancedCsharp-Assignment-01-InvoiceExport-main\AdvancedCsharp-Assignment-01-InvoiceExport-main\invoices\invoice-1005.txt
Export finished in 6.0 s


## Problems with the current design
المشكله في الكود هي استخدام ال Thread.sleep() ودي حاجه طبعا غلط جدا لان الطريقه دي بتعمل بلوك للثريد داخل الفانكشن ويخليه مستني طول المده دي داخل الفاكشن وده طبعا بياثر على الاداء بشكل عام فهيتم وبيخلي البرنامج يحصله تاخير 
-

## What you changed
الحل اني المفروض مخليش اولا الثريد يكون محبوس لحد ما الفانكشن تخلص يبقى هعمل بدل Thread.sleep()=>await Task.delay() وكتابه ال await بتتطلب عمل الانكشن 
async وتكةن بترجع Task لكن السؤال هنا ان عادي يكون العميه تمت في نفس الوقت لاكن احنا هنا استخدمنا الحاجه الصحيحه وهي اني مبسش اي ثريد عندي 
عشان مأثرش على اداء البرنامج 
-
