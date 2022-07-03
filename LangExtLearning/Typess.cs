using LanguageExt;

namespace LangExtLearning
{

    [Union]
    public interface Shape
    {
        Shape Rectangle(float width, float length);
        Shape Circle(float radius);
        Shape Prism(float width, float height);
    }


}
