


What is the Idea?
------------
Main point of this project is to clear **all permission of a folder** and give it to **one** person only so no one else can open it *not even the administrator*.

![Only one user Permission](https://i.imgur.com/duG5djv.jpeg)
Therefore if another user wants to open it, need to use user password.

**Make sure** To encrypt folder so even **changing the owner** of folder has no effect

![Encrypt Folder](https://i.imgur.com/UhGTANN.jpeg)

And it's better to not hide or make it a system file. `attrib +h +s`
Hiding in plain sight works wonder, since they can't even find who owns this.

![Unknown Owner](https://i.imgur.com/RBxV2PB.png)
**This way even `cipher /u /n /h` can not find encrypted file and folders.**

Why?
--------
When you could use Bit-locker or other locking password why this?

 1. General idea is to not use any locking app and just use windows features. 

2. **Hide** something in a way that is not obvious so they can't find a
        application or *easy* to access **setting** which allows them to get an
        idea that something is locked. 

	They Have to get every folder permission to see if they have access or not. (not common to search like this,
        especially if they are in a hurry)

3. Even with strongest password against a adversary everyone talks, therefore **hiding** comes first. (There
        are many hiding technique but i wanted to try my way).

Program
-------------

**Easiest way to invoke to open that Folder is:** in a bat or cmd (not obscure enough)

    runas /user:Hidden "XYplorerPortable.exe e:\Temp\"

    psexec -user Administrator -p Passwd "xcopy a.xml \\server_over_there\c$\A.xml"

But i wanted to hide it even further so i made an app which would deceive a user, right now it is just an idea but this way
        with **Obfuscation** they cant even guess what this app does, let alone ask for any guidance.

This repository
--------------

https://user-images.githubusercontent.com/54465233/200173146-8b116bec-5076-4b53-a257-2553f089917c.mp4


https://github.com/MasoudRahmani/LovePath/raw/master/usage_edit.mp4

Right now main problem is how to use any explorer(for convenience) to open folder.
Windows explorer is limited and on some OS version is not possible therefore i am using third-party explorer (hopefully for now).

* **Explorer++** is a fast and simple application and great portability but might need some tweak. (problem with one drive)
* **XYplorer** is very good but its portability is a problem. (need license)

***Note***
**In order to use* XYPlorer you need to place you license in XY\APP\DefaultData\Data\XYplorer.ini.*

    [Register]
    ; THE FOLLOWING THREE LINES CONTAIN YOUR REGISTRATION INFORMATION. PLEASE KEEP THIS PART PRIVATE.
    Name= some some
    Code= x-x-x-x
    dc=0

**Issue**
---------
***Common Method Errors:***
**Access:**
Since running between users has permission conflict these error may happen.

Application has to run somewhere where user can access third-party program. if you try to run program in `c:\` general it gives you below error.

 - RunasProcess_API

> The directory name is invalid!

 - RunasProcess_Shell

> Access Denied!

**Exception Handling:**
Since it's a third party explorer getting feedback is tricky.
With 
**RunasProcess_API** you can make sure your app would run since otherwise `Process` would throw exception.
**RunasProcess_Shell** since you need to read `console` output and runas command require user to input password, its harder (not implemented) (maybe user psexec )

Hopes
--------------
* Have built-in explorer functionality.
* Run as user in a elevated way so can access any path **Except** Those place where even administrators can **NOT**.
* Maybe as service or tray so That explicit shortcut opens it.


***IF there is another app like this with Exact agenda tell me so I CAN USE IT.***
