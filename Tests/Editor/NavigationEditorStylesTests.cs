using NUnit.Framework;
using YatechStudio.Navigation.Editor;

namespace YatechStudio.Navigation.Tests
{
    public class NavigationEditorStylesTests
    {
        [Test]
        public void LayoutConstants_ArePositive()
        {
            Assert.Greater(NavigationEditorStyles.Padding, 0);
            Assert.Greater(NavigationEditorStyles.ButtonWidth, 0f);
            Assert.Greater(NavigationEditorStyles.RowHeight, 0f);
        }

        [Test]
        public void HeaderStyle_IsNeverNull()
        {
            Assert.IsNotNull(NavigationEditorStyles.HeaderStyle);
        }

        [Test]
        public void IconNames_AreValidBuiltins()
        {
            // A typo in the builtin icon name returns null silently; assert so it
            // surfaces as a failing test instead of an invisible warning glyph.
            Assert.IsNotNull(NavigationEditorStyles.WarningIcon, "console.warnicon.sml missing");
            Assert.IsNotNull(NavigationEditorStyles.ErrorIcon, "console.erroricon.sml missing");
        }
    }
}
