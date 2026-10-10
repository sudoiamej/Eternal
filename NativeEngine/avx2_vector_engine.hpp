#ifndef AVX2_VECTOR_ENGINE_HPP
#define AVX2_VECTOR_ENGINE_HPP

#include <vector>
#include <cstdint>

namespace Eternal::Native {

    class Avx2VectorEngine {
    public:
        Avx2VectorEngine();
        ~Avx2VectorEngine();

        bool IsAvx2Supported() const;
        bool IsAvx512Supported() const;
        void ComputeVectorTelemetry(const float* input, float* output, size_t count);
    private:
        bool m_hasAvx2;
        bool m_hasAvx512;
    };

} // namespace Eternal::Native

#endif // AVX2_VECTOR_ENGINE_HPP
