# Assignment 02 — Answers

Student name:  عبدالرحمن عزت فتوح
Group / ID: #2

## Table booking(before)
Customer-39 booked a table. Left=4
Customer-06 booked a table. Left=4
Customer-31 booked a table. Left=4
Customer-13 booked a table. Left=4
Customer-20 booked a table. Left=4
Customer-14 booked a table. Left=4
Customer-28 booked a table. Left=4
Customer-40 booked a table. Left=4
Customer-23 booked a table. Left=4
Customer-08 booked a table. Left=4
Customer-12 booked a table. Left=4
Customer-11 booked a table. Left=4
Customer-18 booked a table. Left=4
Customer-16 booked a table. Left=4
Customer-17 booked a table. Left=4
Customer-34 booked a table. Left=4
Customer-29 booked a table. Left=4
Customer-01 booked a table. Left=4
Customer-22 booked a table. Left=4
Customer-25 booked a table. Left=4
Customer-26 booked a table. Left=4
Customer-32 booked a table. Left=4
Customer-19 booked a table. Left=4
Customer-35 booked a table. Left=4
Customer-05 booked a table. Left=4
Customer-37 booked a table. Left=4
Customer-27 booked a table. Left=4
Customer-02 booked a table. Left=4
Customer-10 booked a table. Left=4
Customer-30 booked a table. Left=4
Customer-38 booked a table. Left=4
Customer-07 booked a table. Left=4
Customer-36 booked a table. Left=4
Customer-24 booked a table. Left=4
Customer-03 booked a table. Left=4
Customer-33 booked a table. Left=4
Customer-21 booked a table. Left=4
Customer-15 booked a table. Left=4
Customer-09 booked a table. Left=4
Customer-04 booked a table. Left=4

Booked=1, TablesLeft=4

## What is wrong
المشكله كانت اولا في Thread.sleep() ودي شرحناها في اللي قبله وثانيا كانت في عمليه ال increment, decrement 

-

## How you fixed it
pgdkh h,g la;gi f hg async/await وتاني مشكله عملنا lock وحطينا الشرط مع ال increment,decrement جواه وده بيخلي ان ثريد واحد يخش في نفس الوقت فقط ف بيمنع ال 
Race conditioning 







Customer-39 booked a table. Left=4
Customer-06 booked a table. Left=4
Customer-31 booked a table. Left=4
Customer-13 booked a table. Left=4
Customer-20 booked a table. Left=4
Customer-14 booked a table. Left=4
Customer-28 booked a table. Left=4
Customer-40 booked a table. Left=4
Customer-23 booked a table. Left=4
Customer-08 booked a table. Left=4
Customer-12 booked a table. Left=4
Customer-11 booked a table. Left=4
Customer-18 booked a table. Left=4
Customer-16 booked a table. Left=4
Customer-17 booked a table. Left=4
Customer-34 booked a table. Left=4
Customer-29 booked a table. Left=4
Customer-01 booked a table. Left=4
Customer-22 booked a table. Left=4
Customer-25 booked a table. Left=4
Customer-26 booked a table. Left=4
Customer-32 booked a table. Left=4
Customer-19 booked a table. Left=4
Customer-35 booked a table. Left=4
Customer-05 booked a table. Left=4
Customer-37 booked a table. Left=4
Customer-27 booked a table. Left=4
Customer-02 booked a table. Left=4
Customer-10 booked a table. Left=4
Customer-30 booked a table. Left=4
Customer-38 booked a table. Left=4
Customer-07 booked a table. Left=4
Customer-36 booked a table. Left=4
Customer-24 booked a table. Left=4
Customer-03 booked a table. Left=4
Customer-33 booked a table. Left=4
Customer-21 booked a table. Left=4
Customer-15 booked a table. Left=4
Customer-09 booked a table. Left=4
Customer-04 booked a table. Left=4

Booked=1, TablesLeft=4