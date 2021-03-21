/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;

namespace Domain
{

    namespace Sample1
    {

        class Animal
        {
            private readonly string _word;
            private readonly string _name;
            public Animal(string name, string word)
            {
                _name = name;
                _word = word;
            }
            public void Speak(IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : {1} says {2}!", exec.Now, _name, _word);
            }
            public string Name
            {
                get
                {
                    return _name;
                }
            }
        }

        class Dog : Animal
        {
            public Dog(string name) : base(name, "Bark") { }
        }
        class Cat : Animal
        {
            public Cat(string name) : base(name, "Meow") { }
        }

        class Person
        {
            private readonly string _name;
            public Person(string name)
            {
                _name = name;
            }
            public string Name
            {
                get
                {
                    return _name;
                }
            }
        }
    }
}