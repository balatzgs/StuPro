Git:
1. https://git-scm.com/	oder
2. Github
3. etc.

SSH-Key Instructions:
http://doc.gitlab.com/ce/ssh/README.html


Command line instructions

Git global setup
git config --global user.name "$your_name"
git config --global user.email "$your_email"


Create a new repository
git clone git@gitlab.com:tungi/ProFire.git 			

oder

git clone https://gitlab.com/tungi/ProFire.git

cd ProFire
touch README.md
git add README.md
git commit -m "add README"
git push -u origin master


Existing folder or Git repository
cd existing_folder
git init
git remote add origin git@gitlab.com:tungi/ProFire.git
git push -u origin master