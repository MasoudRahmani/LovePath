**Releases** are here or build yourself:

https://github.com/MasoudRahmani/LovePath/tree/master/LovePath_Releases

------------

What is the Idea?
------------
Main point of this project is to clear **all permission of a folder** and give it to **one** person only so no one else can open it *not even the administrator*.

![Only one user Permission](https://i.imgur.com/duG5djv.jpeg)
Therefore if another user wants to open it, need to use user password.

**Make sure** To encrypt folder so even **changing the owner** of folder has no effect

![Encrypt Folder](https://i.imgur.com/UhGTANN.jpeg)

And it's better to not hide or make it a system file. Don't `attrib +h +s`!

Hiding in plain sight works wonder, since they can't even find who owns this.

![Unknown Owner](https://i.imgur.com/RBxV2PB.png)

**This way even `cipher /u /n /h` can not find any encrypted file and folders.**

Why? ( **Most Important: people talks against Force and torture.**)
--------
When you could use Bit-locker / Virtual Disk or any other locking password why this?

 1. General idea is to not use any locking app and just use windows features. 

2. **Hide** something in a way that is not obvious so they can't find a
        application or *easy* to access **setting** which allows them to get an
        idea that something is locked. (finding bitlocker and VD is easy)

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
        with **Obfuscation** they can't even guess what this app does, let alone ask for any guidance.

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

***Remember there are two impersonation:***

1. **Impersonate To Open Config File:** Get which user has access to config file and ask for password. (maybe create my own user in future)

2. **Impersonate specified *User* in config:** to file to open file/folder written in config for `LovePath`.

**The only valid user to Impersonate for *LovePath* is in config, if user doesn't exist or permission is changed you should change config or delete it.**

**Issue**
---------
***Common Method Errors:***

**1. Access:**

Since running between users has permission conflict these error may happen.

Application has to run somewhere where user can access third-party program. if you try to run program in `c:\` general it gives you below error.

 - RunasProcess_API

> The directory name is invalid!

 - RunasProcess_Shell

> Access Denied!

**2. Exception Handling:**

Since it's a third party explorer getting feedback is tricky.
With 

**RunasProcess_API** you can make sure your app would run since otherwise `Process` would throw exception.

**RunasProcess_Shell** since you need to read `console` output and runas command require user to input password, its harder (not implemented) (maybe user psexec )

 ***3. Encryption - Permission of Config File***

 With program premise There is no way to read who is owner of config file or who has permission , Therefore asking for < password and user> multiple time is tedious. 
 
 Current work around is to add "(BUILTIN\\Users)" and assign "Read Permission" access only. so we could get user name only for config file, Secure Path (LovePath) Should have nothing but user.

 We Get required user to login via config so Secure Path don't nead any other permission.


Hopes
--------------
* Have built-in explorer functionality.
* Run as user in a elevated way so can access any path **Except** Those place where even administrators can **NOT**.
* Maybe as service or tray so That explicit shortcut opens it.


***IF there is another app like this with Exact agenda tell me so I CAN USE IT.***
