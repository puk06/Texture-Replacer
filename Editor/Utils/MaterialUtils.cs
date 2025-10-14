using System;
using UnityEditor;
using UnityEngine;

namespace net.puk06.TextureReplacer.Utils
{
    internal static class MaterialUtils
    {
        /// <summary>
        /// �n���ꂽ�}�e���A���̑S�v���p�e�B�����[�v�ŉ񂵂܂��B
        /// Action�ɂ͌��̃e�N�X�`���Ƃ��̃v���p�e�B�����n����܂��B
        /// </summary>
        /// <param name="material"></param>
        /// <param name="action"></param>
        internal static void ForEachTex(Material material, Action<Texture, string> action)
        {
            if (material == null) return;

            Shader shader = material.shader;
            if (shader == null) return;

            int propertyCount = GetPropertyCount(shader);
            if (propertyCount == 0) return;

            for (int i = 0; i < propertyCount; i++)
            {
                if (!IsTexture(shader, i)) continue;

                string propName = ShaderUtil.GetPropertyName(shader, i);
                if (propName == null) continue;

                Texture materialTexture = material.GetTexture(propName);
                if (materialTexture == null) continue;

                action(materialTexture, propName);
            }
        }

        /// <summary>
        /// �n���ꂽ�}�e���A���̑S�v���p�e�B�����[�v�ŉ񂵁A����1�ł��}�e���A���œn���ꂽ�e�N�X�`�����g���Ă�����true��Ԃ��܂��B
        /// </summary>
        /// <param name="material"></param>
        /// <param name="targetTexture"></param>
        /// <returns></returns>
        internal static bool AnyTex(Material material, Texture targetTexture)
        {
            if (material == null) return false;

            Shader shader = material.shader;
            if (shader == null) return false;

            int propertyCount = GetPropertyCount(shader);
            if (propertyCount == 0) return false;

            for (int i = 0; i < propertyCount; i++)
            {
                if (!IsTexture(shader, i)) continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                if (propertyName == null) continue;

                Texture materialTexture = material.GetTexture(propertyName);
                if (materialTexture == null) continue;

                if (materialTexture == targetTexture) return true;
            }

            return false;
        }

        /// <summary>
        /// �V�F�[�_�[�̃v���p�e�B�̐���Ԃ��܂��B
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        internal static int GetPropertyCount(Shader shader)
            => ShaderUtil.GetPropertyCount(shader);

        /// <summary>
        /// �n���ꂽShader�̎w�肳�ꂽindex�̃v���p�e�B��Texture���ǂ�����Ԃ��܂��B
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="propertyIndex"></param>
        /// <returns></returns>
        internal static bool IsTexture(Shader shader, int propertyIndex)
        {
            if (shader == null) return false;
            return ShaderUtil.GetPropertyType(shader, propertyIndex) == ShaderUtil.ShaderPropertyType.TexEnv;
        }
    }
}
