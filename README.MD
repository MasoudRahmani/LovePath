What is the Idea?
------------
Main point of this project is to clear **all permission of a folder** and give it to **one** person only so no one else can open it *not even the administrator*.

![Only one user Permission](https://imgur.com/a/5pZRESF)

Therefore if another user wants to open it, need to use user password.

When you could use Bit-locker or other locking password why this?

 1. General idea is to not use any locking app and just use windows
        feature. 

2. **Hide** something which is not obvious, they can't find a
        application or *easy* to access **setting** which allows them to get an
        idea of something is locked. 

	They Have to get every folder permission to see if they have access or not. (not common to search like this,
        especially if they are in a hurry)

3. Even with strongest password against a adversary everyone talks, therefore **hiding** comes first. (There
        are many hiding technique but i wanted to try my way).

Program
-------------


**Easiest way to invoke to open that Folder is:** in a bat or cmd (not obscure enough)

    runas /user:Hidden "XYplorerPortable.exe e:\Temp\"

But i wanted to hide it even further so i made an app which would deceive a user, right now it is just an idea but this way with **Obfuscation** they cant even guess what this app does, let alone ask for any guidance.

This repository
--------------
Right now main problem is how to use any explorer(for convenience) to open folder.
windows explorer in many scenarios because of user permission returns a error therefore for now i am using third-party explorer.

Error:

    The directory name is invalid

* **Explorer++** is a fast and simple application and great portability but is buggy. (right click crash and ..)
* **XYplorer** is very good but its portability is a problem.

Note:
**In order to use XYPlorer you need to place you licence in XY\APP\DefaultData\Data\XYplorer.ini.**
Like This:

    [Register]
    ; THE FOLLOWING THREE LINES CONTAIN YOUR REGISTRATION INFORMATION. PLEASE KEEP THIS PART PRIVATE.
    Name= some some
    Code= x-x-x-x
    dc=0

**Issue**
---------
Application has to run somewhere where user can access third-party program. if you try to run program in `c:\` general it gives you below error.

    The directory name is invalid.
So run application somewhere else.

Hopes
--------------
* Have built-in explorer functionality.
* Run as user in a elevated way so can access any path **Except** Those place where even administrators can **NOT**.
* Maybe as service or tray so That explicit shortcut opens it.


***IF there is another app like this with Exact agenda tell me so I CAN USE IT.***
