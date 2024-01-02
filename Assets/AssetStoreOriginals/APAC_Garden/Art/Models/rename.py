import os

old_names=[]
new_names=[]

with open('rename.csv','r',encoding='utf-8') as f:
    for line in f.readlines():
        old_names.append(line.split(',')[0].strip())
        new_names.append(line.split(',')[1].strip())

# rename all subfolders and files, including content in files
for root,dirs,files in os.walk('.'):
    for dir in dirs:
        for i in range(len(old_names)):
            if old_names[i] in dir:
                new_name=dir.replace(old_names[i],new_names[i])
                os.rename(os.path.join(root,dir),os.path.join(root,new_name))
                break
    for file in files:
        for i in range(len(old_names)):
            if old_names[i] in file:
                new_name=file.replace(old_names[i],new_names[i])
                os.rename(os.path.join(root,file),os.path.join(root,new_name))
                with open(os.path.join(root,new_name),'r+',encoding='utf-8') as f:
                    content=f.read()
                    f.seek(0,0)
                    f.write(content.replace(old_names[i],new_names[i]))
                break