# What is this package about?
This package contains a framework for dealing with UGC (User Generated Content).

# How do I install this package?

Open the **Package Manager** and click the '+' button.\
Choose **'add package from git URL...'** then paste the **git URL** of this repository and press 'Add'.\
![GitURLButton](https://user-images.githubusercontent.com/45980080/114253417-6f8e0300-99aa-11eb-8744-beaf33319d0c.PNG) \
git URL: https://github.com/Umbrason/Package-System.git

# How do I load assets?
Asset loading is handled in the 'ResourceManager' class. It can either load individual assets using their file path, or load entire packages from their manifest GUID.

# How do I create a new type of asset?
To define your own asset, you have to extend from the 'Package Content' class.
